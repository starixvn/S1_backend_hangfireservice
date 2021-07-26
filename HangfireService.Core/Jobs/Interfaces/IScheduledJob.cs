using HangfireService.Core.Models.Jobs;

namespace HangfireService.Core.Jobs.Interfaces
{
    public interface IScheduledJob
    {
        JobDetailOption JobDetailOption { get; set; }

        void Execute();
    }
}
