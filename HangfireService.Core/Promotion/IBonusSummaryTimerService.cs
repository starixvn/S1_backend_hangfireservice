using HangfireService.Core.Models.DBs;

namespace HangfireService.Core.Promotion
{
    public interface IBonusSummaryTimerService
    {
        Task<int> GetWorkerId(int type);
        BonusSummaryTimer GetBonusSummaryTimer(int type);
        Task<int> SetWorkerId(int type, string memo, int id);
    }
}