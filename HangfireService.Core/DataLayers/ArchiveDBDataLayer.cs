using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace HangfireService.Core.DataLayers
{
    public class ArchiveDBDataLayer
    {
        private readonly MySqlClient _dbClient;

        public ArchiveDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Archive);
        }

        public int InsertArchiveData(string sp, dynamic parameters)
        {
            return _dbClient.Execute(sp, parameters);
        }
    }
}
