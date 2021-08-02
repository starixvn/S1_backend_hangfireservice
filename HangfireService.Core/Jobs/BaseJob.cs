using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

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
