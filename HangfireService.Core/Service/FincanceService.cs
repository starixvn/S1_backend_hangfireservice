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
        
        public int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo, string refTransId, out decimal afterAmount, out byte is1stDepoist, IDbTransaction trans)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                UserName = userName,
                Amount = amount,
                Type = type,
                Memo = memo,
                RefTransId = refTransId
            });

            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Wallet_Set_V2", trans, p);
            is1stDepoist = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal?>("AfterAmount") ?? 0;

            return afterAmount >= 0 ? 1 : 0;
        }
    }
}
