using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Jobs
{
    public class BaseJob
    {
        protected readonly ILogger<BaseJob> _logger;

        public BaseJob(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<BaseJob>>();
        }
    }
}
