using System.Data;
using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using Newtonsoft.Json;

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

        public IEnumerable<dynamic> GetArchiveList(string sp, int liminNum, DateTime archiveTime)
        {
            return _dbClient.Query(sp, parameters: new { LIMINNUM = liminNum, ArchiveTime = archiveTime });
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

        public Task<IEnumerable<TpSettleDelayModel>> GetTpBetRecordDelayByTpId(int TpId)
        {
            return _dbClient.QueryAsync<TpSettleDelayModel>("Service_TpBetRecord_Delay_Get", parameters: new DynamicParameters(new { _TpId = TpId }));
        }

        public IEnumerable<TpBetRecordCheck> GetTpBetRecordCheckData(DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpBetRecord_Check_Get", parameters: new { BeginTime, EndTime }, commandTimeout: 120);
        }

        public IEnumerable<TpRewardRecordCheck> GetTpRewardRecordCheckData(DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpRewardRecordCheck>("Finance_TpRewardRecord_Check_Get", parameters: new { BeginTime, EndTime }, commandTimeout: 120);
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

        public IEnumerable<TpBetRecordCheck> GetTpRewardRecordCheckDataByTpId(int TpId, DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpRewardRecord_Check_ByTpId_Get", parameters: new { _TpId = TpId, _BeginTime = BeginTime, _EndTime = EndTime }, commandTimeout: 120);
        }

        public IEnumerable<TpBetRecordCheck> GetTpBetRecordCheckByGameType(int TpId, int GameType, DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpBetRecord_Check_ByGameType_Get", parameters: new { _TpId = TpId, _GameType = GameType, _BeginTime = BeginTime, _EndTime = EndTime }, commandTimeout: 120);
        }

        public IEnumerable<TpBetRecordCheck> GetTpBetRecordCheckCurrencyIdByTpId(int TpId, int CurrencyId, DateTime BeginTime, DateTime EndTime)
        {
            return _dbClient.Query<TpBetRecordCheck>("Finance_TpBetRecord_Check_CurrencyId_ByTpId_Get", parameters: new { _TpId = TpId, _CurrencyId = CurrencyId, _BeginTime = BeginTime, _EndTime = EndTime }, commandTimeout: 120);
        }

        public IEnumerable<FinanceCheckTransIdData> GetFinanceCheckTransIdHandlingCheck()
        {
            return _dbClient.Query<FinanceCheckTransIdData>("Service_FinanceCheckTransId_Check_Get", parameters: new { _Count = 2, _Limit = 500 });
        }

        public IEnumerable<FinanceCheckTransIdData> GetFinanceCheckTransIdHandlingNotify()
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


        public async Task<IEnumerable<OnlineCountDto>> GetOnlineCount()
            => await _dbClient.QueryAsync<OnlineCountDto>("Service_OnlineUsers_Get_V2", commandTimeout: 120);


        public async Task<IEnumerable<DormantSettingDto>> GetDormantSetting()
            => await _dbClient.QueryAsync<DormantSettingDto>("Service_DormantSetting_Get");

        public async Task<IEnumerable<DormantCustomerDto>> GetDormantCustomer(int siteId, int days, string statusStr)
           => await _dbClient.QueryAsync<DormantCustomerDto>("Service_DormantAccount_Get_V2",
               parameters: new { SiteId = siteId, Day = days, StatusList = statusStr },
               commandTimeout: 180);

        public IEnumerable<dynamic> GetRelayData(string sp, object parameters, int timeoutSeconds)
        {
            return _dbClient.Query(sp, parameters: parameters, commandTimeout: timeoutSeconds);
        }

        public IEnumerable<LeaderboardModel> GetLeaderboardList(int lBCategoryId, int siteId, int currencyId, DateTime startTime, DateTime endTime, int timeoutSeconds, out string timezone)
        {
            var p = new DynamicParameters();
            p.Add("LBCategoryId", lBCategoryId);
            p.Add("SiteId", siteId);
            p.Add("CurrencyId", currencyId);
            p.Add("StartTime", startTime);
            p.Add("EndTime", endTime);
            p.Add("Timezone", direction: ParameterDirection.Output);
            var result = _dbClient.Query<LeaderboardModel>("Member_Leaderboard_Get_V2", p, commandTimeout: timeoutSeconds);

            timezone = p.Get<string>("Timezone");

            return result;
        }

        public IEnumerable<Leaderboard> GetLeaderboardListOld(int lBCategoryId, string eventCode, int siteId,
            int currencyId, int topNum, DateTime startTime, DateTime endTime, int timeoutSeconds)
        {
            var p = new DynamicParameters();
            p.Add("LBCategoryId", lBCategoryId);
            p.Add("EventCode", eventCode);
            p.Add("SiteId", siteId);
            p.Add("CurrencyId", currencyId);
            p.Add("StartTime", startTime);
            p.Add("EndTime", endTime);
            p.Add("TopNum", topNum);
            var result = _dbClient.Query<Leaderboard>("Member_Leaderboard_Get", p, commandTimeout: timeoutSeconds);

            return result;
        }

        public IEnumerable<LeaderboardPointCalculation> GetLeaderboardPointCalculation(int siteId, string date)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_Date", date);
            var result = _dbClient.Query<LeaderboardPointCalculation>("Service_LeaderboardPointCalculation_Get", p);
            return result;
        }

        public LeaderboardTagExcludeList GetLeaderboardTagExcludeList(int siteId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            var result = _dbClient.Query<LeaderboardTagExcludeList>("Service_LeaderboardTagExclude_Get", p)
                .FirstOrDefault();

            return result;
        }

        public IEnumerable<BigWinSetting> GetBigWinSetting(int siteId)
        {
            var p = new DynamicParameters();
            p.Add("SiteId", siteId);

            var result = _dbClient.Query<BigWinSetting>("Service_BigWinSetting_Get", p);
            return result;
        }

        public IEnumerable<CurrencyTimeZone> GetCurrencyTimeZone()
        {
            var result = _dbClient.Query<CurrencyTimeZone>("Service_CurrencyTimeZone_Get");
            return result;
        }

        public IEnumerable<BigWin> GetBigWin(int siteId, int currencyId, DateTime startTime, DateTime endTime, IEnumerable<int> iTpCategoryList)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            p.Add("_StartTime", startTime);
            p.Add("_EndTime", endTime);
            p.Add("_TpCategoryList", JsonConvert.SerializeObject(iTpCategoryList));

            var result = _dbClient.Query<BigWin>("Service_BigWin_Get", p);
            return result;
        }

        public async Task<IEnumerable<string>> GetAllowIp()
        {
            var p = new DynamicParameters();
            p.Add("_Type", 1);
            return await _dbClient.QueryAsync<string>("Service_AllowIP_Get", p);
        }

        public async Task<IEnumerable<string>> GetBlockIp() => await _dbClient.QueryAsync<string>("Service_BlockIP_Get");
        
        public LotteryEvent GetLotteryEventDrawSetting(int siteId , DateTime? date )
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_Date", date);
            var result = _dbClient.QueryFirstOrDefault<LotteryEvent>("Service_LotteryEventDrawSetting_Get", p);
            return result;
        }
        
        public IEnumerable<RaffleTicketSticker> GetRaffleTicketStickerSetting(int siteId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId, dbType: DbType.Int32);
            return _dbClient.Query<RaffleTicketSticker>("Admin_RaffleTicketSticker_Get" , p);
        }
        
        public IEnumerable<LotteryTurnOverSummary> GetLotteryTurnOverSummary(int siteId ,string currencyIds ,DateTime startTime ,DateTime endTime)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyIds", currencyIds);
            p.Add("_StartTime", startTime);
            p.Add("_EndTime", endTime);
            var result = _dbClient.Query<LotteryTurnOverSummary>("Service_LotteryTurnOverSummary_Get", p);
            return result;
        }
        
        public decimal GetCustLotteryTurnOverSummary(int custId ,DateTime startTime ,DateTime endTime)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId);
            p.Add("_StartTime", startTime);
            p.Add("_EndTime", endTime);
            var result = _dbClient.QueryFirst<decimal>("Service_CustLotteryTurnOverSummary_Get", p);
            return result;
        }
            
        public IEnumerable<CustLotteryTicket> GetCustLotteryTicket(int custId ,int roundId ,int type)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId);
            p.Add("_RoundId", roundId);
            p.Add("_Type", type);
            var result = _dbClient.Query<CustLotteryTicket>("Service_CustLotteryTicket_Get", p);
            
            return result;
        }
        
        public IEnumerable<LotteryEventDrawTurnOverSetting> GetLotteryEventDrawTurnOverSetting(int siteId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId, dbType: DbType.Int32);
            return _dbClient.Query<LotteryEventDrawTurnOverSetting>("Service_LotteryEventDrawTurnOverSetting_Get" , p);
        }
        
        public async Task<IEnumerable<Prize>> GetPrizes(int eventId, int currencyId)
        {
            return await _dbClient.QueryAsync<Prize>("Member_DrawPrize_Get", new { _EventId = eventId, _CurrencyId = currencyId });
        }
        
        public IEnumerable<EventRestriction> GetEventRestriction(int siteId)
        {
            return _dbClient.Query<EventRestriction>("Service_EventRestriction_Get", new { _SiteId = siteId });
        }
        
        public LotteryTicketHistory GetLotteryTicketHistory(int siteId ,int roundId)
        {
            return _dbClient.QueryFirstOrDefault<LotteryTicketHistory>("Service_LotteryTicketHistory_Get", new { _SiteId = siteId , _RoundId = roundId });
        }
        
        public IEnumerable<MpaRateDto> GetMPARate(DateTimeOffset starTime, DateTimeOffset endTime , int rateType)
        {
            return _dbClient.Query<MpaRateDto>("Admin_ExchangeRate_Get_V2", new { _RateDateStart = starTime.DateTime , _RateDateEnd = endTime.DateTime ,  _RateType = rateType  });
        }
    }
}
