namespace HangfireService.Core.Models.Options;

public class BonusSummaryOption
{
    public PromotionType PromotionType { get; set; }
}

public enum PromotionType
{
    Promotion, // 1.First Deposit, 2.Re-Deposit, 3.FreeBet, 8.Manual Bonus 9.Retention free bet
    Rebate,
    PrizeDraw,
    VerificationReward 
}