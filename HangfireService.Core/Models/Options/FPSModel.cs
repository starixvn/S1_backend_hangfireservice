using System.Net;

namespace HangfireService.Core.Models.Options
{
    public class FPSOption
    {
        public int SiteId { get; set; }
        public string Subscribers { get; set; }
        public string Token { get; set; }
        public string ApiUrl { get; set; }
        public int Mode { get; set; }
    }
    
    public class FPSResponse
    {
        public int statusCode { get; set; }
        public string errorMessage { get; set; }
        public int totalTrans { get; set; }
        public List<FPSTransaction> trans { get; set; }
    }

    public class FPSTransaction
    {
        public long trackingNumber { get; set; }
        public string account { get; set; }
        public string action { get; set; }
        public string device { get; set; }
        public string deviceType { get; set; }
        public string browser { get; set; }
        public string domain { get; set; }
        public string ip { get; set; }
        public string robotTracking { get; set; }
        public string riskLevel { get; set; }
        public DateTime dateTime { get; set; }
    }
    
    public class FPSRiskLevelResponse
    {
        public int statusCode { get; set; }
        public string errorMessage { get; set; }
        public int totalTrans { get; set; }
        public List<FPSRiskLevel> trans { get; set; }
    }

    public class FPSRiskLevel
    {
        public long id { get; set; }
        public string account { get; set; }
        public string riskLevel { get; set; }
    }

    #region DataLayer
    
    public class SiteTicketKey
    {
        public long TicketKey { get; set; }
    }

    public class CustFPSDto
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public long TrackingNumber { get; set; }
        public string Action { get; set; }
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public string Browser { get; set; }
        public string Domain { get; set; }
        public string IP { get; set; }
        public string RobotTracking { get; set; }
        public string RiskLevel { get; set; }
        public DateTime UpdateTime { get; set; }
    }
    
    public class CustFPSRiskLevelDto
    {
        public int CustId { get; set; }
        public string RiskLevel { get; set; }
    }
    
    #endregion
    
}