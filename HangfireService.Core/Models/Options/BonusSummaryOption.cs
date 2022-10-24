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
    Referral = 5,
    ReferralFreeCredit = 6,
    VipUpgrade = 7,
    Other = 8
}

public enum BonusType
{
    WelcomeBonus = 1,
    ReDeposit = 2,
    FreeBet = 3,
    Rebate = 6,
    CashBack = 7,
    ManualBonus = 8,
    RetentionFreeBet = 9,
    Referral = 10,
    VerificationReward = 11,
    PrizeDraw = 12,
    Other = 13,
    VipUpgrade = 14,
    ReferralFreeCredit = 15
}

public enum TimerType
{
    Time,
    Id
}