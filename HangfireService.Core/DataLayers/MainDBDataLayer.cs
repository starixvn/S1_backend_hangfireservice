using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System.Data;

namespace HangfireService.Core.DataLayers
{
    public class MainDBDataLayer
    {
        private readonly MySqlClient _dbClient;

        public MainDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public Task<IEnumerable<TpDetailTodo>> GetTpDetailTodoListByStatus(int Status)
        {
            return _dbClient.QueryAsync<TpDetailTodo>("Service_TpDetailTodo_Get_V1", parameters: new DynamicParameters(new { Status }));
        }

        public Task<IEnumerable<TpDetailTodo>> GetTpDetailTodoListByTpId(int TpId)
        {
            return _dbClient.QueryAsync<TpDetailTodo>("Service_TpDetailTodo_ByTpId_Get_V1", parameters: new DynamicParameters(new { TpId }));
        }

        public async Task<bool> UpdateTpDetailTodo(TpBetRecordDetail detail)
        {
            var result = await _dbClient.ExecuteAsync("Service_TpDetailTodo_Set", parameters: new DynamicParameters(detail));
            return result > 0;
        }

        public async Task<bool> AddTpBetRecordDetail(TpBetRecordDetail detail)
        {
            var result = await _dbClient.QueryFirstAsync<int>("Service_TpBetRecordDetail_Ins_V2", parameters: new DynamicParameters(detail));
            return result > 0;
        }

        public async Task<int> ResetTpDetailTodo()
        {
            return await _dbClient.ExecuteAsync("Service_TpDetailTodo_Reset");
        }

        public async Task<int> DeleteTpNotImplemented(IEnumerable<int> tpIds)
        {
            var sql = "DELETE FROM TpDetailTodo WHERE TpId NOT IN @tpIds;";
            var parameters = new DynamicParameters();
            parameters.Add("tpIds", tpIds);
            return await _dbClient.ExecuteAsync(sql, CommandType.Text, parameters);
        }

        public int DeleteArchive(string sp, dynamic parameters, IDbTransaction? trans = null)
        {
            return _dbClient.Execute(sp, parameters, trans);
        }

        public int InsertArchive(string sp, dynamic parameters, IDbTransaction? trans = null)
        {
            return _dbClient.QueryFirst<int>(sp, parameters, trans);
        }

        public IDbTransaction BeginTransaction()
        {
            return _dbClient.BeginTransaction();
        }

        public MySqlConnection GetMySqlConnection()
        {
            return _dbClient.GetMySqlConnection();
        }
    }
}
