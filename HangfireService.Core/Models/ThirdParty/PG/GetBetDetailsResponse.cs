using System;
using System.Collections.Generic;
using System.Text;

namespace HangfireService.Core.Models.ThirdParty.PG
{
    public class GetBetDetailsResponse
    {
        public List<Data> data { get; set; }
        public Error error { get; set; }
    }

    public class Data
    {
        public long betId { get; set; }
        public long parentBetId { get; set; }
        public string playerName { get; set; }
        public string currency { get; set; }
        public int gameId { get; set; }
        public int platform { get; set; }
        public int betType { get; set; }
        public int transactionType { get; set; }
        public float betAmount { get; set; }
        public float winAmount { get; set; }
        public float jackpotRtpContributionAmount { get; set; }
        public float jackpotContributionAmount { get; set; }
        public float jackpotWinAmount { get; set; }
        public float balanceBefore { get; set; }
        public float balanceAfter { get; set; }
        public int handsStatus { get; set; }
        public long rowVersion { get; set; }
        public long betTime { get; set; }
        public long betEndTime { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }

}
