using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Service
{
    public class WorldCupDepositDailySummaryService
    {
        private readonly MainDBDataLayer _mainDBDataLayer;
        private readonly ILogger<BaseJob> _logger;

        public WorldCupDepositDailySummaryService(IServiceProvider provider)
        {
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _logger = provider.GetRequiredService<ILogger<BaseJob>>();
        }

        /// <summary>
        /// Job，基本條件 SiteId + CurrencyId + AccountDate  + CustId(可選)
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public async Task RunJobAsync(WorldCupDepositDailySummaryOption option)
        {
            var amountSettings = await GetAmountSetting(option);

            if (!await DepositProcess(option, amountSettings)) return;

            await TicketProcess(option, amountSettings);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 計算玩家充值金額，寫入每日統計資料表
        /// </summary>
        /// <param name="option"></param>
        /// <param name="amountSettings"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> DepositProcess(WorldCupDepositDailySummaryOption option, IEnumerable<DepositAmountSettingDto> amountSettings)
        {
            var dateSetting = await GetDateSetting(option);
            var accountDate = option.AccountDate.Value;

            if (!IsDateValid(accountDate, dateSetting)) return false; // 活動過期就不跑Job
            if (amountSettings.Count() < 3) throw new Exception("amountSetting fail data row less"); // 設定一定是三筆

            var forContinue = true;
            var startIndex = 0;

            while (forContinue)
            {
                var depositSummary = await _mainDBDataLayer.GetDepositInfoSummary(
                    currencyId: option.CurrencyId,
                    siteId: option.SiteId,
                    accountDate: accountDate,
                    startIndex: startIndex,
                    custId: option.CustId);

                forContinue = depositSummary.Count() == 200;

                try
                {
                    // 寫入充值總計
                    await InsertDepositDailySummaryAsync(option, depositSummary, accountDate);
                    // todo 要加入Job log
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);// todo Job log 寫入失敗
                    return false;
                }

                startIndex += 200;
            }

            return true;
        }

        /// <summary>
        /// 派票、收回票
        /// </summary>
        /// <param name="option"></param>
        /// <param name="amountSettings"></param>
        /// <returns></returns>
        public async Task TicketProcess(WorldCupDepositDailySummaryOption option, IEnumerable<DepositAmountSettingDto> amountSettings)
        {
            var startIndex = 0;
            var forContinue = true;

            var insertTickets = new List<CustTicketInfoDto>();
            var rollbackTickets = new List<CustTicketInfoDto>();

            while (forContinue)
            {
                var custDepositInfos = await _mainDBDataLayer.GetWorldCupDepositTotalAmount(option.SiteId,
                    option.CurrencyId,
                    custId: option.CustId,
                    startIndex: startIndex);

                forContinue = custDepositInfos.Count() == 200;

                foreach (var custDepositInfo in custDepositInfos)
                {
                    var userTickets = await _mainDBDataLayer.GetCustTickets(custDepositInfo.CustId); // 取得玩家現在有幾張票
                    rollbackTickets.AddRange(GetRollbackTickets(userTickets, amountSettings, custDepositInfo)); // 要rollback的票

                    // 要新增的票
                    insertTickets.AddRange(
                        from amountSetting in amountSettings.OrderBy(r => r.Amount)
                        where IsGiveTicket(custDepositInfo, amountSetting, userTickets)
                        select GetInsertTicket(amountSetting, custDepositInfo));
                }
                startIndex += 200;
            }

            // 寫入票、收回票
            var tasks = new List<Task<int>>();
            if (insertTickets.Count > 0) tasks.AddRange(insertTickets.Select(item => Task.Run(() => _mainDBDataLayer.InsertWorldCupMemberTicket(item))));
            if (rollbackTickets.Count > 0) tasks.AddRange(rollbackTickets.Select(item => Task.Run(() => _mainDBDataLayer.RollBackWorldCupMemberTicket(item))));

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 帳有問題，重製統計充值紀錄
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="currencyId"></param>
        /// <param name="custId"></param>
        /// <param name="accountDate"></param>
        /// <returns></returns>
        public async Task<int> ReSetWorldCupDepositDailySummary(int siteId, int currencyId, int custId, DateTime accountDate)
        {
            return await _mainDBDataLayer.ReSetWorldCupDepositDailySummary(siteId, currencyId, custId, accountDate);
        }

        /// <summary>
        /// 寫JobLog 紀錄Job執行狀況
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="status"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public async Task<int> InsertJobLog(string jobName, WorldCupJobStatus status, WorldCupDepositDailySummaryOption option)
        {
            return await _mainDBDataLayer.InsertWorldCupJobLog(
                jobName:jobName, 
                siteId:option.SiteId, 
                currencyId:option.CurrencyId,
                custId:option.CustId,
                accountData:option.AccountDate.Value, 
                status:status);
        }

        #region private method

        /// <summary>
        /// 取得需收回的票券
        /// </summary>
        /// <param name="userTickets"></param>
        /// <param name="amountSettings"></param>
        /// <param name="custDepositInfo"></param>
        /// <returns></returns>
        private List<CustTicketInfoDto> GetRollbackTickets(IEnumerable<WorldCupMemberTicketInfoDto> userTickets,
            IEnumerable<DepositAmountSettingDto> amountSettings,
            CustDepositTotalAmountDto custDepositInfo)
        {
            if (!userTickets.Any()) return new List<CustTicketInfoDto>();

            return (from amountSetting in amountSettings
                    where custDepositInfo.DepositAmountTotal < amountSetting.Amount &&
                          userTickets.Any(r => r.TicketType == amountSetting.TicketType)
                    select new CustTicketInfoDto()
                    {
                        SiteId = amountSetting.SiteId,
                        CustId = custDepositInfo.CustId,
                        CurrencyId = amountSetting.CurrencyId,
                        TicketType = amountSetting.TicketType,
                        TicketStatus = (byte)TicketStatus.InValid,
                    }).ToList();
        }

        /// <summary>
        /// 需要寫入的票
        /// </summary>
        /// <param name="amountSetting"></param>
        /// <param name="custDepositInfo"></param>
        /// <param name="ticketStatus"></param>
        /// <returns></returns>
        private CustTicketInfoDto GetInsertTicket(DepositAmountSettingDto amountSetting,
            CustDepositTotalAmountDto custDepositInfo)
        {
            return new CustTicketInfoDto()
            {
                SiteId = amountSetting.SiteId,
                CustId = custDepositInfo.CustId,
                CurrencyId = amountSetting.CurrencyId,
                TicketType = amountSetting.TicketType,
                TicketStatus = (byte)TicketStatus.Valid,
            };
        }

        /// <summary>
        /// 判斷玩家原充值金額是否已經達發票門檻
        /// </summary>
        /// <param name="custDepositInfo"></param>
        /// <param name="amountSetting"></param>
        /// <param name="userTickets"></param>
        /// <returns></returns>
        private static bool IsGiveTicket(CustDepositTotalAmountDto custDepositInfo,
            DepositAmountSettingDto amountSetting,
            IEnumerable<WorldCupMemberTicketInfoDto> userTickets)
        {
            return custDepositInfo.DepositAmountTotal >= amountSetting.Amount
                   && userTickets.All(r => r.TicketType != amountSetting.TicketType);
        }

        /// <summary>
        /// 檢查是否還在發票的活動期間
        /// </summary>
        /// <param name="accountDate"></param>
        /// <param name="dateSetting"></param>
        /// <returns></returns>
        private static bool IsDateValid(DateTime accountDate, WorldCupDateSettingDto dateSetting)
        {
            return accountDate.Date > dateSetting.DepositStartTime.Date &&
                   accountDate.Date <= dateSetting.DepositEndTime.AddDays(1).Date;
        }

        /// <summary>
        /// 取得世界盃充值金額門檻設定
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<IEnumerable<DepositAmountSettingDto>> GetAmountSetting(WorldCupDepositDailySummaryOption option)
        {
            var amountSettings = await _mainDBDataLayer.GetDepositAmountSetting();
            var amountSetting = amountSettings.Where(r => r.SiteId == option.SiteId && r.CurrencyId == option.CurrencyId);
            if (amountSetting == null)
                throw new Exception($"not found amountSetting，SiteId:{option.SiteId}，CurrencyId:{option.CurrencyId}"); ;
            return amountSetting;
        }

        /// <summary>
        /// 取得世界盃日期設定設定
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<WorldCupDateSettingDto> GetDateSetting(WorldCupDepositDailySummaryOption option)
        {
            var dateSettings = await _mainDBDataLayer.GetWorldCupDateSetting();
            var dateSetting = dateSettings.FirstOrDefault(r => r.SiteId == option.SiteId && r.CurrencyId == option.CurrencyId);
            if (dateSetting == null)
                throw new Exception($"not found dateSettings，SiteId:{option.SiteId}，CurrencyId:{option.CurrencyId}");
            return dateSetting;
        }

        /// <summary>
        /// 寫入玩家充值統計表
        /// </summary>
        /// <param name="option"></param>
        /// <param name="depositSummary"></param>
        /// <param name="accountDate"></param>
        /// <returns></returns>
        private async Task InsertDepositDailySummaryAsync(WorldCupDepositDailySummaryOption option, IEnumerable<DepositInfoSummaryDto> depositSummary,
            DateTime accountDate)
        {
            var insertDepositDailySummary = depositSummary.Select(r =>
                new CustWorldCupDepositDailySummaryDto
                {
                    SiteId = option.SiteId,
                    CurrencyId = option.CurrencyId,
                    CustId = r.CustId,
                    DepositAmount = r.DepositAmount,
                    AccountDate = accountDate,
                    Status = 1
                }).ToList();

            var tasks = new List<Task<int>>();
            tasks.AddRange(insertDepositDailySummary.Select(item => Task.Run(() => _mainDBDataLayer.InsertDepositDailySummary(item))));

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }

        #endregion
    }
}
