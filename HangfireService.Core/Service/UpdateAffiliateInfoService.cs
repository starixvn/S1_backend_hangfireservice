using HangfireService.Core.Models.Affiliate;
using HangfireService.Core.Models.Enums;

using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Service
{
    public class UpdateAffiliateInfoService
    {
        private readonly AffiliateApi _affiliateApi;
        private readonly CustomerService _customerService;
        private readonly ILogger<UpdateAffiliateInfoService> _logger;
        private const int PageSize = 2000;
        private readonly List<int> SiteList = new() { 1, 2, 3 };

        public UpdateAffiliateInfoService(AffiliateApi affiliateApi, CustomerService customerService, ILogger<UpdateAffiliateInfoService> logger)
        {
            _affiliateApi = affiliateApi;
            _customerService = customerService;
            _logger = logger;
        }

        public async Task<int> RunJobAsync()
        {
            foreach (var site in SiteList)
            {
                try
                {
                    var token = await _affiliateApi.AuthGetAccessToken();
                    if (string.IsNullOrEmpty(token)) return -1;

                    var affFromVN = await _affiliateApi.QueryAffDetailInfoList(1, PageSize, (SiteShortName)site, token);
                    var affInSite = await _customerService.GetNotUpdatedAffiliateInfo(site);

                    var affList = affFromVN.Where(x => !affInSite.Any(y => y.AffiliateName == x.AffiliateName)).Select(z => new AffiliateInfo
                    {
                        SiteId = site,
                        AffiliateCode = z.AffiliateCode,
                        AffiliateName = z.AffiliateName,
                        ParentAffiliate = string.IsNullOrEmpty(z.ParentAffiliateName) ? "" : z.ParentAffiliateName,
                        UserName = z.PlayerName
                    }).ToList();
                    await _customerService.CreateOrUpdateAffiliateInfo(affList);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return 1;
        }
    }
}
