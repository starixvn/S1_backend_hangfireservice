using HangfireService.Core.Models.Enums;
using System.Text;

namespace HangfireService.Core.Models.DBs
{
    public class OTPCheck
    {
        public int CurrencyId { get; set; }
        public int OTPType { get; set; }
        public int CustId { get; set; }     
        public string OTPProvider { get; set; }
        public string Request_id { get; set; }
        public string OTPNum { get; set; }
        public string OTPResponse { get; set; }
        public DateTime CreateTime { get; set; }
        public long StartUnixTime { get; set; }
        public int TimeOutMin { get; set; }
        private TimeSpan _offset { get; set; } = TimeSpan.Zero;
        public long CreateTimeToUnix => new DateTimeOffset(CreateTime, _offset).ToUnixTimeSeconds();

        public bool IsExpire 
        {
            get
            {
                return DateTimeOffset.UtcNow > DateTimeOffset.FromUnixTimeSeconds(StartUnixTime).AddMinutes(TimeOutMin);
            }
        }

        public OTPCheckResult GetResult(OTPStatus status, string message = null)
        {
            var result = new StringBuilder(OTPResponse);
            if (message != null)
            {
                result.Append($" || {message}");
            }

            if (!string.IsNullOrEmpty(OTPNum) && IsExpire)
            {
                result.Append($" || OTP is Expire.");
            }

            return new OTPCheckResult(this)
            {
                OTPStatus = (int)status,
                OTPResponse = result.ToString(),
            };
        }
    }

    public class OTPCheckResult
    {
        public int CurrencyId { get; set; }
        public int OTPType { get; set; }
        public int CustId { get; set; }
        public string OTPProvider { get; set; }
        public string Request_id { get; set; }
        public string OTPNum { get; set; }
        public int OTPStatus { get; set; }
        public string OTPResponse { get; set; }

        public OTPCheckResult(OTPCheck oTPCheck)
        {
            CurrencyId = oTPCheck.CurrencyId;
            OTPType = oTPCheck.OTPType;
            CustId = oTPCheck.CustId;
            OTPProvider = oTPCheck.OTPProvider.ToString();
            Request_id = oTPCheck.Request_id;
            OTPNum = oTPCheck.OTPNum;
        }
    }
}
