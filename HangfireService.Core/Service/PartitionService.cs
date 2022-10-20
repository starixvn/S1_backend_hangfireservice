using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Service
{
    public class PartitionService
    {
        private readonly MySqlClient _mainDbClient;

        public PartitionService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public IEnumerable<T> Query<T>(DateTime beginTime, DateTime EndTime, string spName, object parameters)
        {
            var tasks = new List<Task<IEnumerable<T>>>();
            foreach (var partitionKey in beginTime.GetPartitionRange(EndTime))
            {
                var p = new DynamicParameters(parameters);
                p.Add("YYYYMM", partitionKey);
                tasks.Add(_mainDbClient.QueryAsync<T>(spName, parameters: p));
            }

            Task.WhenAll(tasks);

            var result = new List<T>();
            foreach (var item in tasks)
            {
                result.AddRange(item.Result);
            }
            return result;
        }

        public IEnumerable<Task<IEnumerable<T>>> QueryAsync<T>(DateTime beginTime, DateTime EndTime, string spName, object parameters, int? commandTimeout = null)
        {
            var tasks = new List<Task<IEnumerable<T>>>();
            foreach (var partitionKey in beginTime.GetPartitionRange(EndTime))
            {
                var p = new DynamicParameters(parameters);
                p.Add("YYYYMM", partitionKey);
                tasks.Add(_mainDbClient.QueryAsync<T>(spName, parameters: p, commandTimeout: commandTimeout));
            }
            return tasks;      
        }






    }
}
