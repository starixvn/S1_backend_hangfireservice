using HangfireService.Core.Models.Enums;
using Newtonsoft.Json;

namespace HangfireService.Core.Models.Finance
{
    public class TpBetRecordCheck
    {
        public int TpId { get; set; }

        public int CurrencyId { get; set; }

        public decimal Stake { get; set; }

        public decimal ValidBet { get; set; }

        public decimal WinlostAmount { get; set; }

        public decimal Revenue { get; set; }

        public decimal ProgressiveBet { get; set; }

        public decimal ProgressiveWin { get; set; }

        public string TpCurrencyId { get; set; }

        public FinanceCheckResult CheckData(FinanceCheckType checkType, TpBetRecordCheck? check)
        {
            var result = new List<string>();
            if (check == null)
            {
                result.Add($"TpBetRecord not found CurrencyId:{CurrencyId} data.");
                return new FinanceCheckResult(checkType, CurrencyId, result);
            }
            var stakeDiff = Stake - check.Stake;
            var validBetDiff = ValidBet - check.ValidBet;
            var revenueDiff = Revenue - check.Revenue;
            var winlostAmountDiff = WinlostAmount - check.WinlostAmount;
            var progressiveBetDiff = ProgressiveBet - check.ProgressiveBet;
            var progressiveWinDiff = ProgressiveWin - check.ProgressiveWin;


            if (stakeDiff != 0)
            {
                result.Add($"Stake 差額:{stakeDiff:f4}");
            }
            if (validBetDiff != 0)
            {
                result.Add($"ValidBet 差額:{validBetDiff:f4}");
            }
            if (winlostAmountDiff != 0)
            {
                result.Add($"WinlostAmount 差額:{winlostAmountDiff:f4}");
            }
            if (revenueDiff != 0)
            {
                result.Add($"Revenue 差額:{revenueDiff:f4}");
            }
            if (progressiveBetDiff != 0)
            {
                result.Add($"ProgressiveBet 差額:{progressiveBetDiff:f4}");
            }
            if (progressiveWinDiff != 0)
            {
                result.Add($"ProgressiveWin 差額:{progressiveWinDiff:f4}");
            }

            return new FinanceCheckResult(checkType, CurrencyId, result);
        }
    }

    public class TpRewardRecordCheck
    {
        public int TpId { get; set; }

        public int CurrencyId { get; set; }

        public decimal WinlostAmount { get; set; }
    }

    public class BettransCheckData : TpBetRecordCheck
    {
        public decimal Payout { get; set; }

        public FinanceCheckResult CheckData(FinanceCheckType checkType, TpDailySummaryCheckData? check)
        {
            var result = new List<string>();
            if (check == null)
            {
                result.Add($"TpDailySummary not found CurrencyId:{CurrencyId} data.");
                return new FinanceCheckResult(checkType, CurrencyId, result);
            }

            var baseCheck = base.CheckData(checkType, check);
            var payoutDiff = Payout - check.Payout;
            if (payoutDiff != 0)
            {
                result.Add($"Payout 差額:{payoutDiff:f4}");
                if (baseCheck != null)
                {
                    baseCheck.ErrorList.AddRange(result);
                }
                else
                {
                    baseCheck = new FinanceCheckResult(checkType, CurrencyId, result);
                }
            }

            return baseCheck;
        }
    }

    public class TpDailySummaryCheckData : TpBetRecordCheck
    {
        public decimal Payout { get; set; }

        public decimal Reward { get; set; }
    }

    public class TpBetRecordCheckTransId
    {
        public int TpId { get; set; }

        public int CurrencyId { get; set; }

        public string TransId { get; set; }

        public decimal Stake { get; set; }

        public decimal ValidBet { get; set; }

        public decimal WinlostAmount { get; set; }

        public decimal Revenue { get; set; }

        public decimal ProgressiveBet { get; set; }

        public decimal ProgressiveWin { get; set; }

        public int SettlementType { get; set; }

        public int TicketStatus { get; set; }

        public ErrorDetail CheckTransIdData(int tpId, FinanceCheckTransIdType checkType, TpBetRecordCheckTransId check)
        {
            decimal checkStake = 0;
            decimal checkValidBet = 0;
            decimal checkRevenue = 0;
            decimal checkWinlostAmount = 0;
            decimal checkProgressiveBet = 0;
            decimal checkProgressiveWin = 0;
            var checkTransId = TransId;

            if (check != null)
            {
                checkStake = check.Stake;
                checkValidBet = check.ValidBet;
                checkRevenue = check.Revenue;
                checkWinlostAmount = check.WinlostAmount;
                checkProgressiveBet = check.ProgressiveBet;
                checkProgressiveWin = check.ProgressiveWin;
            }

            var stakeDiff = Stake - checkStake;
            var validBetDiff = ValidBet - checkValidBet;
            var revenueDiff = Revenue - checkRevenue;
            var winlostAmountDiff = WinlostAmount - checkWinlostAmount;
            var progressiveBetDiff = ProgressiveBet - checkProgressiveBet;
            var progressiveWinDiff = ProgressiveWin - checkProgressiveWin;

            var match = true;

            if (stakeDiff != 0 && Math.Abs(stakeDiff) > 0.01m)
            {
                match = false;
            }
            if (validBetDiff != 0 && Math.Abs(validBetDiff) > 0.01m)
            {
                match = false;
                if (tpId == 36)
                {
                    // 因第三方資料無法計算出ValidBet，故不檢查
                    match = true;
                }
            }
            if (winlostAmountDiff != 0 && Math.Abs(winlostAmountDiff) > 0.01m)
            {
                match = false;
            }
            if (revenueDiff != 0 && Math.Abs(revenueDiff) > 0.01m)
            {
                match = false;
            }
            if (progressiveBetDiff != 0 && Math.Abs(progressiveBetDiff) > 0.01m)
            {
                match = false;
            }
            if (progressiveWinDiff != 0 && Math.Abs(progressiveWinDiff) > 0.01m)
            {
                match = false;
            }

            if (!match)
            {
                var errorDetail = new ErrorDetail
                {
                    TransId = checkTransId,
                    Stake = $"差額:{stakeDiff:f4}",
                    ValidBet = $"差額:{validBetDiff:f4}",
                    WinlostAmount = $"差額:{winlostAmountDiff:f4}",
                    Revenue = $"差額:{revenueDiff:f4}",
                    ProgressiveBet = $"差額:{progressiveBetDiff:f4}",
                    ProgressiveWin = $"差額:{progressiveWinDiff:f4}",
                };

                return errorDetail;
            }
            else
            {
                return null;
            }
        }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class BettransCheckTransIdData : TpBetRecordCheckTransId
    {
        public string TpCurrencyId { get; set; }

        public decimal Payout { get; set; }
    }
}
