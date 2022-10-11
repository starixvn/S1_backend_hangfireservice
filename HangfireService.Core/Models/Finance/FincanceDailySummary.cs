using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Models.Finance
{
    public class FincanceDailySummary
    {
        public SummaryType SummaryType { get; set; }
        public DateTime SummaryDate { get; set; }
        public int TpId { get; set; }
        public int CurrencyId { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal Payout { get; set; }
        public decimal Revenue { get; set; }
        public decimal WinlostAmount { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public decimal Reward { get; set; }
    }
}
