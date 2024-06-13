using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.WinLossSummary;
using MySql.Data.MySqlClient;
using System.Data;
namespace HangfireService.Core.DataLayers;

public class ReportCenterDataLayer
{
    public readonly MySqlClient _dbClient;

    public ReportCenterDataLayer(IServiceProvider provider)
    {
        _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.ReportCenter);
    }

    public MySqlConnection GetMySqlConnection()
    {
        return _dbClient.GetMySqlConnection();
    }

    #region WinLossSummary

    public int InsertWinLossData(string sp, object parameters, IDbTransaction trans)
    {
        return _dbClient.Execute(sp, trans, parameters);
    }

    public int DeleteWinLossData(string sp, object parameters, IDbTransaction trans)
    {
        return _dbClient.Execute(sp, trans, parameters);
    }

    #endregion
}
