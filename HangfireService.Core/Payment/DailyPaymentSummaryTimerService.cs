using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Payment;

public class DailyPaymentSummaryTimerService : IDailyPaymentSummaryTimerService
{
    private readonly MySqlClient _reportDb;
    private readonly MySqlClient _mainDb;

    public DailyPaymentSummaryTimerService(IEnumerable<MySqlClient> sqlClients)
    {
        _mainDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
        _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
    }

    public async Task<DateTime> GetWorkerTime(int type)
    {
        return await _reportDb.QueryFirstAsync<DateTime>("Service_DailyPaymentSummaryTimerTime_Get", new { _Type = type });
    }

    public async Task<int> GetWorkerId(int type)
    {
        return await _reportDb.QueryFirstAsync<int>("Service_DailyPaymentSummaryTimerId_Get", new { _Type = type });
    }

    public async Task<int> SetWorkerTime(int type, DateTime time)
    {
        return await _mainDb.ExecuteAsync("Service_DailyPaymentSummaryTimerTime_Set" , new { _Type = type , _Time = time});
    }
    public async Task<int> SetWorkerId(int type, int id)
    {
        return await _mainDb.ExecuteAsync("Service_DailyPaymentSummaryTimerId_Set" , new { _Type = type , _Id = id});
    }
}