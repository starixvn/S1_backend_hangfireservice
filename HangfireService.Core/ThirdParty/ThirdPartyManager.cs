using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;

using Microsoft.Extensions.Logging;

namespace HangfireService.Core.ThirdParty
{
    public class ThirdPartyManager
    {
        private readonly ILogger<ThirdPartyManager> _logger;

        private readonly ReportDBDataLayer _reportDBDataLayer;

        private readonly MainDBDataLayer _mainDBDataLayer;

        public ThirdPartyManager(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ThirdPartyManager>>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        }

        public IEnumerable<ThirdPartyModel> GetAllTpList()
        {
            return _reportDBDataLayer.GetAllTpList();
        }
    }
}



