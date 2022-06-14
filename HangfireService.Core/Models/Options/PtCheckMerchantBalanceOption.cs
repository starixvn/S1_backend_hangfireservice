namespace HangfireService.Core.Models.Options
{
    public class PtCheckMerchantBalanceOption
    {
        /// <summary>
        /// 第三方遊戲Id
        /// </summary>
        public int TpId { get; set; }

        /// <summary>
        /// 站台Id
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// 幣別Id、"CNY"，"VND"..etc
        /// </summary>
        public string TpCurrencyId { get; set; }

        /// <summary>
        /// 小於這個額度要發 Alter 至 LetsTalk 群組通知客服需要加值該幣別商戶餘額
        /// </summary>
        public decimal LessThanBalance { get; set; }
    }
}
