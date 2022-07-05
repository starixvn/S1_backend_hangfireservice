namespace HangfireService.Core.Models.DBs
{
    public class WorldCupDateSettingDto
    {
        public int SiteId { get; set; }

        public int CurrencyId { get; set; }

        public DateTime DepositStartTime { get; set; }

        public DateTime DepositEndTime { get; set; }

        public DateTime TicketOpenTime { get; set; }

        public DateTime TicketCloseTime { get; set; }

        /// <summary>
        /// 時區
        /// </summary>
        public string TimeZone { get; set; }

    }
}
