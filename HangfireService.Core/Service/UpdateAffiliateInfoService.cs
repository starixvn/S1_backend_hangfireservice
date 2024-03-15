using HangfireService.Core.Models.Affiliate;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Service
{
    public class UpdateAffiliateInfoService
    {
        private readonly AffiliateApi _affiliateApi;
        private readonly CustomerService _customerService;
        private readonly ILogger<UpdateAffiliateInfoService> _logger;
        private const int PageSize = 2000;

        public UpdateAffiliateInfoService(AffiliateApi affiliateApi, CustomerService customerService, ILogger<UpdateAffiliateInfoService> logger)
        {
            _affiliateApi = affiliateApi;
            _customerService = customerService;
            _logger = logger;
        }

        public async Task<int> RunJobAsync(int siteId, string siteName)
        {
            try
            {
                var token = await _affiliateApi.AuthGetAccessToken();
                if (string.IsNullOrEmpty(token)) return -1;

                var affFromVn = await _affiliateApi.QueryAffDetailInfoList(1, PageSize, siteName, token);
                var affInSite = await _customerService.GetNotUpdatedAffiliateInfo(siteId);

                var affList = affFromVn.Where(x => affInSite.All(y => y.AffiliateName != x.AffiliateName)).Select(z => new AffiliateInfo
                {
                    SiteId = siteId,
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
            }
            
            return 1;
        }
    }
}
