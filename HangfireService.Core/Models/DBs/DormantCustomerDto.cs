namespace HangfireService.Core.Models.DBs;

public class DormantCustomerDto
{
    public int CustId { get; set; }
    public string UserName { get; set; }
    public int SiteId { get; set; }
    public DateTime LastLockTime { get; set; }
}
