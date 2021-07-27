using HangfireService.Core.Models.Jobs;
using System.Threading.Tasks;

namespace HangfireService.Core.Jobs.Interfaces
{
    public interface IScheduledJob
    {
        JobDetailOption JobDetailOption { get; set; }

        Task Execute();
    }
}
