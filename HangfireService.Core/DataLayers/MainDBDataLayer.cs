using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<int> DeleteTpNotImplemented(IEnumerable<string> tpIds)
        {
            var sql = "DELETE FROM TpDetailTodo WHERE TpId NOT IN @tpIds;";
            var parameters = new DynamicParameters();
            parameters.Add("tpIds", tpIds);
            return await _dbClient.ExecuteAsync(sql, CommandType.Text, parameters);
        }
    }
}
