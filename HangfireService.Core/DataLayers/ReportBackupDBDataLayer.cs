using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.WinLossSummary;
using System.Data;
namespace HangfireService.Core.DataLayers;

public class ReportBackupDBDataLayer
{
    public readonly MySqlClient _dbClient;

    public ReportBackupDBDataLayer(IServiceProvider provider)
    {
        _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.ReportBackupDB);
    }

    public async Task<IEnumerable<WinLossSourceModel>> GetWinLossSourceDataAsync(string sp, object parameters, int timeoutSeconds)
    {
        return await _dbClient.QueryAsync<WinLossSourceModel>(sp, parameters: parameters, commandTimeout: timeoutSeconds);
    }

    public async Task<IEnumerable<WinLoss9WCommissionSourceModel>> GetWinLoss9WCommissionSourceDataAsync(DateTime startTime, DateTime endTime)
    {
        var p = new DynamicParameters();
        p.Add("_StartTime", startTime, dbType: DbType.DateTime);
        p.Add("_EndTime", endTime, dbType: DbType.DateTime);

        return await _dbClient.QueryAsync<WinLoss9WCommissionSourceModel>("WinLoss_9WCommission_Get", p);
    }
}
