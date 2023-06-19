using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.Affiliate
{
    public class AffiliateInfo
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string AffiliateName { get; set; }
        public string ParentAffiliate { get; set; }
        public string AffiliateCode { get; set; }
        public string UserName { get; set; }
    }

}
