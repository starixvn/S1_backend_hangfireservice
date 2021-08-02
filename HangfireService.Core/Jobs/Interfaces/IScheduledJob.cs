using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Models.Jobs;
using System.Threading.Tasks;

namespace HangfireService.Core.Jobs.Interfaces
{
    public interface IScheduledJob
    {
        string  JobName { get; set; }

        JobDetailOption JobDetailOption { get; set; }

        Task Execute();
    }
}
