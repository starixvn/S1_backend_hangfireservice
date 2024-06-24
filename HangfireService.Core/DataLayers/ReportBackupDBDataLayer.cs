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

    public IEnumerable<WinLossSourceModel> GetWinLossSourceData(string sp, object parameters, int timeoutSeconds)
    {
        return _dbClient.Query<WinLossSourceModel>(sp, parameters: parameters, commandTimeout: timeoutSeconds);
    }

    public IEnumerable<WinLoss9WCommissionSourceModel> GetWinLoss9WCommissionSourceData(DateTime startTime, DateTime endTime)
    {
        var p = new DynamicParameters();
        p.Add("_StartTime", startTime, dbType: DbType.DateTime);
        p.Add("_EndTime", endTime, dbType: DbType.DateTime);

        return _dbClient.Query<WinLoss9WCommissionSourceModel>("WinLoss_9WCommission_Get", p);
    }
}
