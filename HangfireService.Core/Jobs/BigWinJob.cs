using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Security.Policy;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("BigWinJob", queue: "Report")]
    public class BigWinJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly MainDBDataLayer _mainDBDataLayer;
        private readonly List<CurrencyTimeZone> _currencyTimeZoneList;

        public BigWinJob(IServiceProvider provider) : base(provider)
        {
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _currencyTimeZoneList = _reportDBDataLayer.GetCurrencyTimeZone().ToList();
        }
        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var option = JsonConvert.DeserializeObject<BigWinOption>(jobOption.Parameters);
                var bigWinSettingList = _reportDBDataLayer.GetBigWinSetting(option.SiteId).Where(x => x.Status == (int)BigWinSettingEnum.Open).ToList();

                foreach (var bigWinSetting in bigWinSettingList)
                {
                    if (bigWinSetting.SettleTime.AddDays(1) < DateTime.UtcNow)
                    {
                        var currencyTimeZone = _currencyTimeZoneList.Find(x => x.CurrencyId == bigWinSetting.CurrencyId);
                        var offSet = TryParseTimezoneOffset(currencyTimeZone.TimeZone);
                        var (startDate, endDate, settleDate) = GetDateRange(offSet);
                        //取得有贏錢的下注紀錄
                        var bigWinList = _reportDBDataLayer.GetBigWin(option.SiteId, bigWinSetting.CurrencyId, startDate, endDate, bigWinSetting.ITpCategoryList).ToList();
                        //取出滿足中獎倍數與最低贏得金額
                        bigWinList = bigWinList.FindAll(x => x.WinlostAmount / x.ValidBet >= bigWinSetting.MultipleOfWinning && x.WinlostAmount >= bigWinSetting.MinOfWinning);
                        //取出贏得金額最大的一筆下注紀錄
                        var bigWinBet = bigWinList.MaxBy(x => x.WinlostAmount);
                        if (bigWinBet != null)
                        {
                            _mainDBDataLayer.InsertBigWinInfo(new BigWinInfo()
                            {
                                SiteId = option.SiteId,
                                CurrencyId = bigWinSetting.CurrencyId,
                                CustId = bigWinBet.CustId,
                                TpId = bigWinBet.TpId,
                                TpCategory = bigWinBet.CategoryId,
                                TpGameType = bigWinBet.GameType,
                                GameID = bigWinBet.GameID,
                                Time = settleDate,
                                WinlostAmount = bigWinBet.WinlostAmount
                            });

                            _logger.LogInformation($"SiteId:{option.SiteId} \r\n CurrencyId:{bigWinSetting.CurrencyId} \r\n WinlostAmount:{bigWinBet.WinlostAmount} \r\n have BigWin in {startDate}~{endDate}");
                        }

                        //更新SettleTime
                        _mainDBDataLayer.SetBigWinSetting(option.SiteId, bigWinSetting.CurrencyId, endDate);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }
        private TimeSpan TryParseTimezoneOffset(string offsetString)
        {
            try
            {
                if (offsetString.StartsWith("+") || offsetString.StartsWith("-"))
                {
                    var hours = int.Parse(offsetString.Substring(1, 2));
                    var minutes = int.Parse(offsetString.Substring(4, 2));

                    var offset = offsetString.StartsWith("+")
                        ? TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(minutes))
                        : TimeSpan.FromHours(-hours).Subtract(TimeSpan.FromMinutes(minutes));

                    return offset;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TryParseTimezoneOffset ExceptionMessage：{ex.Message}");
            }

            throw new Exception($"offsetString Format Fail，offsetString:{offsetString}");
        }
        private (DateTime startDate, DateTime endDate, DateTime settleDate) GetDateRange(TimeSpan offset)
        {
            //取得該幣別當下的日子
            var currentDate = DateTimeOffset.UtcNow.AddHours(offset.Hours).AddMinutes(offset.Minutes).Date;
            //計算的日子為前一天
            var settleDate = currentDate.AddDays(-1);
            var startDate = currentDate.AddDays(-1).AddHours(-offset.Hours).AddMinutes(-offset.Minutes);
            var endDate = currentDate.AddSeconds(-1).AddHours(-offset.Hours).AddMinutes(-offset.Minutes);

            return (startDate, endDate, settleDate);
        }
        public enum BigWinSettingEnum
        {
            Close = 0,
            Open = 1
        }
    }
}
