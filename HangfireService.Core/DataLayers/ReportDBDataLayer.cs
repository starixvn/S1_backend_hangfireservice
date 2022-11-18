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

        public IEnumerable<TpCurrency> GetTpCurrency(int TpId)
        {
            return _dbClient.Query<TpCurrency>("Service_TpCurrency_Get", parameters: new { TpId });
        }

        public IEnumerable<OTPCheck> GetOTPCheckList()
        {
            return _dbClient.Query<OTPCheck>("Service_OTPLog_Check");
        }

        public Customer GetCustInfo(int CustId)
        {
            return _dbClient.QueryFirst<Customer>("Customer_Get", parameters: new { CustId });
        }
    }
}
