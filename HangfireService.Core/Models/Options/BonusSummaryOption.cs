namespace HangfireService.Core.Models.Options;

public class BonusSummaryOption
{
    public WorkerType WorkerType { get; set; }
    public TimerType TimerType { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Id { get; set; }
}

public class WorkerTime
{
    public DateTime StartTime { get; set; }
    public DateTime EndDate { get; set; }
}

public enum WorkerType
{
    Promotion = 1, // 1.First Deposit, 2.Re-Deposit, 3.FreeBet, 7.CashBack , 8.Manual Bonus 9.Retention free bet
    Rebate = 2,
    PrizeDraw = 3,
    VerificationReward = 4,
    Referral = 10,
    ReferralFreeCredit = 11,
    VipUpgrade = 14,
    Other = 15
}

public enum TimerType
{
    Time,
    Id
}