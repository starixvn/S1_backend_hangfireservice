using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HangfireService.Core.DataLayers
{
    public class WagerDBDataLayer
    {
        private readonly MySqlClient _dbClient;

        public WagerDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

    }
}
