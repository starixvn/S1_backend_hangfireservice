using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;

namespace HangfireService.Core.DataLayers
{
    public class ReportDBDataLayer
    {
        private readonly MySqlClient _dbClient;

        public ReportDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<ThirdPartyModel> GetAllTpList()
        {
            return _dbClient.Query<ThirdPartyModel>("Service_TpList_GetAll");
        }

        public IEnumerable<dynamic> GetArchiveList(string sp, int liminNum)
        {
            return _dbClient.Query(sp, parameters: new { LIMINNUM = liminNum });
        }

        public IEnumerable<LicenseReport> GetBalanceForLicense(int siteId, DateTime time, string currencyIds)
        {
            return _dbClient.Query<LicenseReport>("Service_BalanceForLicense_Get_V2", parameters: new { _Time = time, _SiteId = siteId, _CurrencyIds = currencyIds });
        }

        public Task<IEnumerable<TpGameTypeModel>> GetGameTypeByCategoryId(int CategoryId)
        {
            return _dbClient.QueryAsync<TpGameTypeModel>("Service_GameTypeByCategoryId_Get", parameters: new DynamicParameters(new { CategoryId }));
        }

        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingLiveCasinoByTpGameType(int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_LiveCasino_Bettrans_Get", parameters: new DynamicParameters(new { TpGameType, BeginTime, EndTime }), commandTimeout: 120);
        }

        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingSlotByTpGameType(int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_Slot_Bettrans_Get", parameters: new DynamicParameters(new { TpGameType, BeginTime, EndTime }), commandTimeout: 120);
        }

        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingFishByTpGameType(int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_Fish_Bettrans_Get", parameters: new DynamicParameters(new { TpGameType, BeginTime, EndTime }), commandTimeout: 120);
        }

        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingLotteryByTpGameType(int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_Lottery_Bettrans_Get", parameters: new DynamicParameters(new { TpGameType, BeginTime, EndTime }), commandTimeout: 120);
        }

        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingPokerByTpGameType(int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_Poker_Bettrans_Get", parameters: new DynamicParameters(new { TpGameType, BeginTime, EndTime }), commandTimeout: 120);
        }

        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingTpUnsettleByTpGameType(int TpId, int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_TpUnsettle_Get", parameters: new DynamicParameters(new { TpId, TpGameType, BeginTime, EndTime }), commandTimeout: 120);
        }

        public IEnumerable<TpBetRecordCheck> GetTpBetRecordCheckData(DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpBetRecord_Check_Get", parameters: new { BeginTime, EndTime }, commandTimeout: 120);
        }

        public IEnumerable<TpBetRecordCheckTransId> GetTpBetRecordCheckTransIdData(DateTime BeginTime, DateTime EndTime, int TpId, TimeOption TimeOption = 0, string TransId = "[]")
        {
            return _dbClient.Query<TpBetRecordCheckTransId>("Finance_TpBetRecord_Check_TransId_Get", parameters: new { _BeginTime = BeginTime, _EndTime = EndTime, _TpId = TpId, _TimeOption = TimeOption, _TransIds = TransId }, commandTimeout: 120);
        }

        public IEnumerable<TpBetRecordCheck> GetTpBetRecordCheckSettlementTypeByTpId(int TpId, DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpBetRecord_Check_SettlementType_ByTpId_Get", parameters: new { _TpId = TpId, _BeginTime = BeginTime, _EndTime = EndTime }, commandTimeout: 120);
        }

        public IEnumerable<TpBetRecordCheck> GetTpBetRecordCheckDataByTpId(int TpId, DateTime BeginTime, DateTime EndTime, TicketStatusExcludeOption TicketStatusExcludeOption = 0, TimeOption TimeOption = 0)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpBetRecord_Check_ByTpId_Get", parameters: new { _TpId = TpId, _BeginTime = BeginTime, _EndTime = EndTime, _TicketStatusExcludeOption = TicketStatusExcludeOption, _TimeOption = TimeOption }, commandTimeout: 120);
        }

        public IEnumerable<TpBetRecordCheck> GetTpBetRecordCheckByGameType(int TpId, int GameType, DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpBetRecord_Check_ByGameType_Get", parameters: new { _TpId = TpId, _GameType = GameType, _BeginTime = BeginTime, _EndTime = EndTime }, commandTimeout: 120);
        }

        public IEnumerable<TpBetRecordCheck> GetTpBetRecordCheckCurrencyIdByTpId(int TpId, int CurrencyId, DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpBetRecord_Check_CurrencyId_ByTpId_Get", parameters: new { _TpId = TpId, _CurrencyId = CurrencyId, _BeginTime = BeginTime, _EndTime = EndTime }, commandTimeout: 120);
        }

        public IEnumerable<FinanceCheckTransIdData> GetFinanceCheckTransIdHandling()
        {
            return _dbClient.Query<FinanceCheckTransIdData>("Service_FinanceCheckTransId_Get");
        }

        public IEnumerable<TpCurrency> GetTpCurrency(int TpId)
        {
            return _dbClient.Query<TpCurrency>("Service_TpCurrency_Get", parameters: new { TpId });
        }

        public Task<IEnumerable<Banner>> GetBanner()
        {
            return _dbClient.QueryAsync<Banner>("Service_BannerExpired_Get");
        }

        public IEnumerable<TpBettingCheckModel> GetTpBettingCheck()
        {
            return _dbClient.Query<TpBettingCheckModel>("Service_TpBettingMonitor_Get");
        }

        public IEnumerable<TpLastBetting> GetLastBettingTime(DateTime startTime, DateTime endTime)
        {
            return _dbClient.Query<TpLastBetting>("Service_TpLastBetting_Get", parameters: new { _StartTime = startTime, _EndTime = endTime });
        }

        public IEnumerable<OTPCheck> GetOTPCheckList()
        {
            return _dbClient.Query<OTPCheck>("Service_OTPLog_Check");
        }

        public Customer GetCustInfo(int CustId)
        {
            return _dbClient.QueryFirst<Customer>("Customer_Get", parameters: new { CustId });
        }

        #region Statistical-Online Count

        public async Task<IEnumerable<OnlineCountDto>> GetOnlineCount()
            => await _dbClient.QueryAsync<OnlineCountDto>("Service_OnlineUsers_Get_V2", commandTimeout: 120);

        #endregion

        #region Statistical-Customer Auto Lock

        public async Task<IEnumerable<DormantSettingDto>> GetDormantSetting()
            => await _dbClient.QueryAsync<DormantSettingDto>("Service_DormantSetting_Get");

        public async Task<IEnumerable<DormantCustomerDto>> GetDormantCustomer(int siteId, int days, string statusStr)
           => await _dbClient.QueryAsync<DormantCustomerDto>("Service_DormantAccount_Get_V2",
               parameters: new { SiteId = siteId, Day = days, StatusList = statusStr },
               commandTimeout: 180);

        #endregion

        public IEnumerable<dynamic> GetRelayData(string sp, object parameters, int timeoutSeconds)
        {
            return _dbClient.Query(sp, parameters: parameters, commandTimeout: timeoutSeconds);
        }

        public IEnumerable<Leaderboard> GetLeaderboardList(int lBCategoryId, string eventCode, int siteId, int currencyId, int topNum, DateTime startTime, DateTime endTime)
        {
            var p = new DynamicParameters();
            p.Add("LBCategoryId", lBCategoryId);
            p.Add("EventCode", eventCode);
            p.Add("SiteId", siteId);
            p.Add("CurrencyId", currencyId);
            p.Add("StartTime", startTime);
            p.Add("EndTime", endTime);
            p.Add("TopNum", topNum);
            var result = _dbClient.Query<Leaderboard>("Member_Leaderboard_Get", p);
            return result;
        }

    }
}
