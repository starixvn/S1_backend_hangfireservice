using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;
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

        public Task<IEnumerable<TpDetailTodo>> GetTpDetailTodoListByTpId(TpDetailOption option)
        {
            return _dbClient.QueryAsync<TpDetailTodo>("Service_TpDetailTodo_ByTpId_Get_V2", parameters: new DynamicParameters(new { option.TpId, option.LiminNum }));
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

        public int DeleteArchive(string sp, dynamic parameters, IDbTransaction trans)
        {
            return _dbClient.Execute(sp, trans,parameters);
        }
        public int DeleteArchive(string sp, dynamic parameters)
        {
            return _dbClient.ExecuteAsync(sp, parameters: parameters);
        }

        public int InsertArchive(string sp, dynamic parameters, IDbTransaction trans)
        {
            return _dbClient.QueryFirst<int>(sp, trans, parameters );
        }

        public MySqlConnection GetMySqlConnection()
        {
            return _dbClient.GetMySqlConnection();
        }

        public async Task<int> ResetTpUnsettle()
        {
            return await _dbClient.ExecuteAsync("Service_TpUnsettle_Reset");
        }
    }
}
