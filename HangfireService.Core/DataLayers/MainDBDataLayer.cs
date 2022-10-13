using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;
using MySql.Data.MySqlClient;
using System.Data;

namespace HangfireService.Core.DataLayers
{
    public class MainDBDataLayer
    {
        private readonly MySqlClient _dbClient;

        public MainDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public MySqlConnection GetMySqlConnection()
        {
            return _dbClient.GetMySqlConnection();
        }

        public async Task<int> ResetTpUnsettle()
        {
            return await _dbClient.ExecuteAsync("Service_TpUnsettle_Reset");
        }

        #region TpDetail

        public Task<IEnumerable<TpDetailTodo>> GetTpDetailTodoListByStatus(int Status)
        {
            return _dbClient.QueryAsync<TpDetailTodo>("Service_TpDetailTodo_Get_V1", parameters: new DynamicParameters(new { Status }));
        }

        public Task<IEnumerable<TpDetailTodo>> GetTpDetailTodoListByTpId(TpDetailOption option)
        {
            return _dbClient.QueryAsync<TpDetailTodo>("Service_TpDetailTodo_ByTpId_Get_V2", parameters: new DynamicParameters(new { option.TpId, option.LiminNum }));
        }

        public async Task<bool> UpdateTpDetailTodo(TpBetRecordDetail detail)
        {
            var result = await _dbClient.ExecuteAsync("Service_TpDetailTodo_Set", parameters: new DynamicParameters(detail));
            return result > 0;
        }

        public async Task<bool> AddTpBetRecordDetail(TpBetRecordDetail detail)
        {
            var result = await _dbClient.QueryFirstAsync<int>("Service_TpBetRecordDetail_Ins_V2", parameters: new DynamicParameters(detail));
            return result > 0;
        }

        public async Task<int> ResetTpDetailTodo()
        {
            return await _dbClient.ExecuteAsync("Service_TpDetailTodo_Reset");
        }

        #endregion

        #region Archive
       
        public int DeleteArchive(string sp, dynamic parameters, IDbTransaction trans)
        {
            return _dbClient.Execute(sp, trans, parameters);
        }
        public int DeleteArchive(string sp, dynamic parameters)
        {
            return _dbClient.ExecuteAsync(sp, parameters: parameters);
        }

        public int InsertArchive(string sp, dynamic parameters, IDbTransaction trans)
        {
            return _dbClient.QueryFirst<int>(sp, trans, parameters);
        }

        #endregion

        #region WorldCup

        /// <summary>
        /// 取得世界盃充值，投票設定時間
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<WorldCupDateSettingDto>> GetWorldCupDateSetting()
        {
            return await _dbClient.QueryAsync<WorldCupDateSettingDto>("Service_WorldCupDateSetting_Get");
        }

        /// <summary>
        /// 取得世界盃衝值設定金額表
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DepositAmountSettingDto>> GetDepositAmountSetting()
        {
            return await _dbClient.QueryAsync<DepositAmountSettingDto>("Member_DepositAmountSetting_Get");
        }

        /// <summary>
        /// 取得世界盃玩家前一天充值資訊
        /// </summary>
        /// <param name="currencyId">幣別</param>
        /// <param name="siteId">品牌站台</param>
        /// <param name="accountDate">抓帳日期，通常是抓前一天</param>
        /// <param name="startIndex">從第幾筆開始抓</param>
        /// <param name="pageSize">一次抓幾筆</param>
        /// <param name="custId">丟0，不分 user，全抓，byUser才丟值</param>
        /// <returns></returns>
        public async Task<IEnumerable<DepositInfoSummaryDto>> GetDepositInfoSummary(int currencyId,
            int siteId,
            DateTime accountDate,
            int startIndex,
            int pageSize = 200,
            int custId = 0)
        {
            var p = new DynamicParameters();
            p.Add("_CurrencyId", currencyId, dbType: DbType.Int64);
            p.Add("_SiteId", siteId, dbType: DbType.Int64);
            p.Add("_AccountDate", accountDate, dbType: DbType.DateTime);
            p.Add("_CustId", custId, dbType: DbType.Int64);
            p.Add("_PageSize", pageSize, dbType: DbType.Int64);
            p.Add("_StartIndex", startIndex, dbType: DbType.Int64);

            return await _dbClient.QueryAsync<DepositInfoSummaryDto>("Service_DepositInfoDailySummary_Get", p);
        }

        /// <summary>
        /// 取得玩家的票券
        /// </summary>
        /// <param name="custId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<WorldCupMemberTicketInfoDto>> GetCustTickets(int custId)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId, dbType: DbType.Int64);
            return await _dbClient.QueryAsync<WorldCupMemberTicketInfoDto>("Member_WorldCupMemberTicketInfo_Get", p);
        }

        /// <summary>
        /// 新增玩家充值總帳
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<int> InsertDepositDailySummary(CustWorldCupDepositDailySummaryDto data)
        {
            return _dbClient.ExecuteAsync("Service_CustWorldCupDepositDailySummary_Ins", new
            {
                _SiteId = data.SiteId,
                _CurrencyId = data.CurrencyId,
                _CustId = data.CustId,
                _DepositAmount = data.DepositAmount,
                _AccountDate = data.AccountDate.AddDays(-1),
                _Status = data.Status
            });
        }

        /// <summary>
        /// 取得玩家充值總帳
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<CustDepositTotalAmountDto>> GetWorldCupDepositTotalAmount(int siteId, int currencyId, int custId,
            int pageSize = 200, int startIndex = 0)
        {
            var p = new DynamicParameters();
            p.Add("_CurrencyId", currencyId, dbType: DbType.Int64);
            p.Add("_SiteId", siteId, dbType: DbType.Int64);
            p.Add("_CustId", custId, dbType: DbType.Int64);
            p.Add("_PageSize", pageSize, dbType: DbType.Int64);
            p.Add("_StartIndex", startIndex, dbType: DbType.Int64);
            return await _dbClient.QueryAsync<CustDepositTotalAmountDto>("Service_WorldCupDepositAmount_Get", p);
        }

        /// <summary>
        /// 新增玩家票券
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<int> InsertWorldCupMemberTicket(CustTicketInfoDto data)
        {
            return _dbClient.ExecuteAsync("Service_WorldCupMemberTicketInfo_Ins", new
            {
                _SiteId = data.SiteId,
                _CustId = data.CustId,
                _CurrencyId = data.CurrencyId,
                _TicketType = data.TicketType,
                _TicketStatus = data.TicketStatus
            });
        }

        /// <summary>
        /// 收回玩家的票
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<int> RollBackWorldCupMemberTicket(CustTicketInfoDto data)
        {
            return _dbClient.ExecuteAsync("Service_WorldCupMemberTicketInfo_Set", new
            {
                _CustId = data.CustId,
                _TicketType = data.TicketType,
                _TicketStatus = data.TicketStatus
            });
        }

        /// <summary>
        /// 帳有問題，重製統計充值紀錄
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="currencyId"></param>
        /// <param name="custId"></param>
        /// <param name="accountData"></param>
        /// <returns></returns>
        public async Task<int> ReSetWorldCupDepositDailySummary(int siteId, int currencyId, int custId, DateTime accountData)
        {
            return await _dbClient.ExecuteAsync("Service_CustWorldCupDepositDailySummary_Set", new
            {
                _SiteId = siteId,
                _CurrencyId = currencyId,
                _CustId = custId,
                _AccountDate = accountData.AddDays(-1)
            });
        }

        public async Task<int> InsertWorldCupJobLog(string jobName, int siteId, int currencyId, int custId, DateTime accountData, WorldCupJobStatus status)
        {
            return await _dbClient.ExecuteAsync("Service_WorldCupJobLog_Ins", new
            {
                _jobName = jobName,
                _SiteId = siteId,
                _CurrencyId = currencyId,
                _CustId = custId,
                _AccountDate = accountData.AddDays(-1),
                _status = (byte)status
            });
        }
        #endregion

        public int SetTpBetRecordRelayData(DateTime date, int timeoutSeconds)
        {
            var parameters = new
            {
                StartTime = date,
                EndTime = date.AddDays(1),
            };
            return _dbClient.QueryFirst<int>("Relay_TpBetRecord_Set_V3", parameters, commandTimeout: timeoutSeconds);
        }
        
        public async Task<int> ExpiredBanner(string bannerName, int siteId, int currencyId)
        {
            return await _dbClient.ExecuteAsync("Service_BannerExpired_Set", new
            {
                _BannerName = bannerName,
                _SiteId = siteId,
                _CurrencyId = currencyId
            });
        }
        
        public int SetLastBettingTime(int siteId, int currencyId, int tpId , int gameType)
        {
            var parameters = new
            {
                _SiteId = siteId,
                _CurrencyId = currencyId,
                _TpId = tpId,
                _GameType = gameType
            };
            return _dbClient.Execute("Service_TpBettingMonitorSetting_Set", parameters);
        }
    }
}

