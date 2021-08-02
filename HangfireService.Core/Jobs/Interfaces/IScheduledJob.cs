using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Models.Jobs;
using System.Threading.Tasks;

namespace HangfireService.Core.Jobs.Interfaces
{
    public interface IScheduledJob
    {
        string ClassName { get; set; }

        Task Execute(JobDetail jobDetail, params string[] parameters);
    }
}
