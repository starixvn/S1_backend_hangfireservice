using HangfireService.Core.Models.DBs;

namespace HangfireService.Core.Promotion
{
    public interface IBonusSummaryTimerService
    {
        BonusSummaryTimer GetBonusSummaryTimer(int type);
        Task<int> SetWorkerTimer(int type, string memo, int id, DateTime? time);
    }
}