using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

        public Task<IEnumerable<TpDetailTodo>> GetTpDetailTodoList(int Status)
        {   
            return _dbClient.QueryAsync<TpDetailTodo>("Service_TpDetailTodo_Get", parameters: new DynamicParameters(new { Status }));
        }

        public async Task<bool> UpdateTpDetailTodo(TpBetRecordDetail detail)
        {
            var result = await _dbClient.ExecuteAsync("Service_TpDetailTodo_Set", parameters: new DynamicParameters(detail));
            return result > 0;
        }
    }
}
