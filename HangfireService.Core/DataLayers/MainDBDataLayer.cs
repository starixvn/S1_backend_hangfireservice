using System.Data;

using Dapper;

using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.MpaExchangeRate;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;

using MySql.Data.MySqlClient;

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

        public async Task<int> ExpiredBanner(string bannerName, int siteId, int currencyId)
        {
            return await _dbClient.ExecuteAsync("Service_BannerExpired_Set", new
            {
                _BannerName = bannerName,
                _SiteId = siteId,
                _CurrencyId = currencyId
            });
        }

        public int SetLastBettingTime(int siteId, int currencyId, int tpId, int gameType, DateTime? lastBettingTime, bool isExecute)
        {
            var parameters = new
            {
                _SiteId = siteId,
                _CurrencyId = currencyId,
                _TpId = tpId,
                _GameType = gameType,
                _LastBettingTime = lastBettingTime,
                _IsExecute = isExecute
            };
            return _dbClient.Execute("Service_TpBettingMonitorSetting_Set", parameters);
        }

        public IEnumerable<TpMaintain> GetTpMaintainList(int siteId)
        {
            return _dbClient.Query<TpMaintain>("TpMaintain_Get", new DynamicParameters(new { SiteId = siteId }));
        }

        #region BonusSummary

        public int BonusSummaryPromotionSet(DateTime startTime, DateTime endTime, IDbTransaction trans)
        {
            return _dbClient.Execute("Service_BonusSummaryForPromotion_Set", trans, parameters: new DynamicParameters(new { _StartTime = startTime, _EndTime = endTime }));
        }

        public int BonusSummaryVerificationSet(DateTime startTime, DateTime endTime, IDbTransaction trans)
        {
            return _dbClient.Execute("Service_BonusSummaryForVerification_Set", trans, parameters: new DynamicParameters(new { _StartTime = startTime, _EndTime = endTime }));
        }

        public int BonusSummaryDrawPrizeSet(int id, IDbTransaction trans, out int maxId)
        {
            var props = new DynamicParameters();
            props.Add("_Id", id);
            props.Add("_MaxId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = _dbClient.Execute("Service_BonusSummaryForDrawPrize_Set", trans, props);
            maxId = props.Get<int?>("_MaxId") ?? 0;

            return result;
        }

        public int BonusSummaryRebateSet(int id, IDbTransaction trans, out int maxId)
        {
            var props = new DynamicParameters();
            props.Add("_Id", id);
            props.Add("_MaxId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = _dbClient.Execute("Service_BonusSummaryForRebate_Set", trans, props);
            maxId = props.Get<int?>("_MaxId") ?? 0;

            return result;
        }

        public int SetBonusSummaryTimer(int type, int id, DateTime? time, string memo, IDbTransaction trans)
        {
            return _dbClient.Execute("Service_Timer_Set", trans, parameters: new { _Type = type, _StartTime = time, _Memo = memo, _Id = id });
        }

        #endregion

        public int InsertRelayData(string sp, object parameters, IDbTransaction trans)
        {
            return _dbClient.Execute(sp, trans, parameters);
        }

        public int DeleteRelayData(string sp, object parameters, IDbTransaction trans)
        {
            return _dbClient.Execute(sp, trans, parameters);
        }

        public int SetOTPStatus(OTPCheckResult result)
        {
            return _dbClient.Execute("Service_OTPLog_Set", result);
        }

        #region Statistical-Online Count

        public async Task<int> SetOnlineCount(IEnumerable<OnlineCountDto> onlineCountDtos)
            => await _dbClient.ExecuteAsync("Service_OnlineCount_Set_V2", onlineCountDtos);

        #endregion

        #region Statistical-Customer Auto Lock

        public async Task<int> SetCustomerStatus(int custId, int status)
            => await _dbClient.ExecuteAsync("Service_LockDormantAccount_Set", new { CustId = custId, Status = status });

        public async Task<int> SetCustNotes(int custId, string note, string @operator)
            => await _dbClient.ExecuteAsync("Admin_CustNotes_Ins", new { CustId = custId, Note = note, Operator = @operator });

        public async Task<int> SetDormantSetting(int siteId)
            => await _dbClient.ExecuteAsync("Service_DormantSetting_Set", new { SiteId = siteId });

        #endregion

        #region Statistical-Cust Closing Balance

        public async Task<int> SetCustClosingBalance()
        {
            var parameters = new DynamicParameters();
            parameters.Add("TotalCount", dbType: DbType.Int32, direction: ParameterDirection.InputOutput);

            await _dbClient.ExecuteAsync("Service_CustClosingBalance_Set", parameters);

            return parameters.Get<int?>("TotalCount") ?? 0;
        }

        #endregion

        /// <summary>
        /// 新增玩家充值總帳
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<int> InsertDailyExchangeRate(MpaUpdateExchangeRateService.DailyExchangeRateResponse data)
        {
            return _dbClient.ExecuteAsync("Service_ExchangeRate_Set_V2", new
            {
                _RateDate = data.RateDate,
                _RateType = (int)ExchangeRateTypeEnum.Daily,
                _CurrencyId = data.CurrencyId,
                _CurrencyName = data.CurrencyName,
                _TargetCurrencyId = data.TargetCurrencyId,
                _TargetCurrencyName = data.TargetCurrencyName,
                _TargetCurrencyRate = data.TargetCurrencyRate
            });
        }

        public Task<int> InsertMonthlyExchangeRate(string data)
        {
            return _dbClient.ExecuteAsync("Service_ExchangeRate_Set_Batch", new
            {
                _Infos = data
            });
        }

        public int InsertFinanceCheckTransId(FinanceCheckTransIdData financeCheckTransIdData)
        {
            return _dbClient.Execute("Service_FinanceCheckTransId_Ins", new
            {
                _TpId = financeCheckTransIdData.TpId,
                _TransId = financeCheckTransIdData.TransId,
                _FinanceCheckTransIdType = financeCheckTransIdData.FinanceCheckTransIdType,
                _CheckTime = financeCheckTransIdData.CheckTime,
                _ErrorData = financeCheckTransIdData.ErrorData
            });
        }

        public int DeleteFinanceCheckTransId(long SQ)
        {
            return _dbClient.Execute("Service_FinanceCheckTransId_Del", new
            {
                _SQ = SQ
            });
        }

        public int UpdateFinanceCheckTransIdCount(long SQ)
        {
            return _dbClient.Execute("Service_FinanceCheckTransId_Count_Set", new
            {
                _SQ = SQ
            });
        }

        public int InsertBigWinInfo(BigWinInfo bigWinInfo)
        {
            return _dbClient.Execute("Service_BigWinInfo_Set", new
            {
                _SiteId = bigWinInfo.SiteId,
                _CurrencyId = bigWinInfo.CurrencyId,
                _CustId = bigWinInfo.CustId,
                _TpId = bigWinInfo.TpId,
                _TpCategory = bigWinInfo.TpCategory,
                _TpGameType = bigWinInfo.TpGameType,
                _GameID = bigWinInfo.GameID,
                _Time = bigWinInfo.Time,
                _WinlostAmount = bigWinInfo.WinlostAmount,
            });
        }
        public int SetBigWinSetting(int siteId, int currencyId, DateTime settleTime)
        {
            return _dbClient.Execute("Service_BigWinSetting_Set", new
            {
                _SiteId = siteId,
                _CurrencyId = currencyId,
                _SettleTime = settleTime
            });
        }

        public int InsertCustFPS(CustFPSDto custFPSDto)
        {
            return _dbClient.Execute("Service_CustFPS_Ins", new
            {
                _SiteId = custFPSDto.SiteId,
                _CustId = custFPSDto.CustId,
                _UserName = custFPSDto.UserName,
                _TrackingNumber = custFPSDto.TrackingNumber,
                _Action = custFPSDto.Action,
                _DeviceId = custFPSDto.DeviceId,
                _DeviceType = custFPSDto.DeviceType,
                _Browser = custFPSDto.Browser ,
                _Domain = custFPSDto.Domain,
                _IP = custFPSDto.IP,
                _RobotTracking = custFPSDto.RobotTracking,
                _RiskLevel = custFPSDto.RiskLevel,
                _UpdateTime = custFPSDto.UpdateTime
            });
        }
        
        public int InsertCustFPSRiskLevel(CustFPSRiskLevelDto custFPSDto)
        {
            return _dbClient.Execute("Service_CustFPSRiskLevel_Ins", new
            {
                _CustId = custFPSDto.CustId,
                _RiskLevel = custFPSDto.RiskLevel,
            });
        }
    }
}

