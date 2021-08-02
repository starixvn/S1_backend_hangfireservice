using Hangfire.Dashboard;

namespace HangfireService.Extensions.ActionFilters
{
    public class AdminSiteAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
            //var httpContext = context.GetHttpContext();
            //return httpContext.User.Identity.IsAuthenticated;
        }
    }
}
