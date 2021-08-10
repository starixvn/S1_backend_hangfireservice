using Hangfire.Dashboard;

namespace HangfireService.Extensions.ActionFilters
{
    public class LocalAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}
