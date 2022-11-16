using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.DBs;

public class GetTpBetRecordForLeadBoardDto
{
    public DateTime Date { get; set; }
    public string EventCode { get; set; }
    public int CategoryId { get; set; }
    public int CustId { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public decimal Turnover { get; set; }
    public int TurnoverSiteRank { get; set; }
    public int TurnoverCategoryRank { get; set; }
    public decimal WinlostAmount { get; set; }
}
