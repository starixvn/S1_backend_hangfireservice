using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace HangfireService.Core.DataLayers
{
    public class MainDBDataLayer
    {
        private readonly MySqlClient _dbClient;

        public MainDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
    }
}
