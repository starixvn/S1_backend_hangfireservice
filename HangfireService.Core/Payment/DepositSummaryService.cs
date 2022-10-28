using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Payment;

namespace HangfireService.Core.Payment;

public class DepositSummaryService : IDepositSummaryService
{
    private readonly MySqlClient _reportDb;
    private readonly MySqlClient _mainDb;

    public DepositSummaryService(IEnumerable<MySqlClient> sqlClients)
    {
        var mySqlClients = sqlClients.ToList();
        _mainDb = mySqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
        _reportDb = mySqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
    }
    
    public async Task<IEnumerable<GetDepositLogummaryDto>> GetDepositInfoSummary(int id)
    {
        return await _reportDb.QueryAsync<GetDepositLogummaryDto>("Service_DepositLogSummary_Get", new { _Id = id });
    }

    public async Task<int> SetDailyDepositSummary(IEnumerable<SetDailyDepositSummaryDto> depositSummary)
    {
        return await _mainDb.ExecuteAsync("Service_DailyDepositSummary_Set", depositSummary);
    }
}