namespace HangfireService.Core.Models.DBs
{
    public class CustDepositTotalAmountDto
    {
        public int CustId { get; set; }

        /// <summary>
        /// 充值金額
        /// </summary>
        public decimal DepositAmountTotal { get; set; }
    }
}
