using System.Data;
using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;

namespace HangfireService.Core.Service
{
    public class FincanceService
    {
        private readonly MySqlClient _mainDbClient;


        public FincanceService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

        }

        public Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummary(int SummaryType, int TpId, DateTime BeginTime, DateTime EndTime)
        {
            return _mainDbClient.QueryAsync<TpDailySummaryCheckData>("Finance_Tp_DailySummary_Get", new { SummaryType, TpId, BeginTime, EndTime });
        }

        public int SetFinanceTpDailySummary(IEnumerable<FincanceDailySummary> dailySummary)
        {
            return _mainDbClient.Execute("Finance_Tp_DailySummary_Set", dailySummary);
        }
        
        public Task<IEnumerable<BankFinancialTransferAccount>> GetBankFinancialTransferAccountList()
        {
            return _mainDbClient.QueryAsync<BankFinancialTransferAccount>("Service_BankFinancialTransferAccount_GetList");
        }

        
        public int SetFinancialAccountClosingBalance(int id, string yyyymm)
        {
            var p = new DynamicParameters(new
            {
                _FinancialAccountId = id,
                _YYYYMM = yyyymm
            });
            
            p.Add("_Result", direction: ParameterDirection.Output);
            
            _ = _mainDbClient.Execute("Service_FinancialAccount_ClosingBalance_Set", p);

            return p.Get<int?>("_Result") ?? -9;
        }
    }
}
