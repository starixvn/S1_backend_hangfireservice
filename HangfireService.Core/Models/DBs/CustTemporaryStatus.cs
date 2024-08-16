namespace HangfireService.Core.Models.DBs;

public class CustTemporaryStatus
{
    public int CustId { get; set; }
    public int Status { get; set; }
    public DateTime ExpiredTime { get; set; }
}