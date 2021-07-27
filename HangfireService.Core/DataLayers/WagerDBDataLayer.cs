using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireService.Core.DataLayers
{
    public class WagerDBDataLayer
    {
        private readonly MySqlClient _dbClient;

        public WagerDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public async Task<bool> AddTpBetRecordDetail(TpBetRecordDetail detail)
        {
            var result = await _dbClient.QueryFirstAsync<int>("Service_TpBetRecordDetail_Ins", parameters: new DynamicParameters(detail));
            return result > 0;
        }
    }
}
