using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HangfireService.Core.DataLayers
{
    public class ReportDBDataLayer
    {   
        private readonly MySqlClient _dbClient;

        public ReportDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<ThirdPartyModel> GetAllTpList()
        {
            return _dbClient.Query<ThirdPartyModel>("Service_TpList_GetAll");
        }

        public IEnumerable<dynamic> GetArchiveList(string sp, int liminNum)
        {
            return _dbClient.Query(sp, parameters: new { LIMINNUM = liminNum });
        }


    }
}
