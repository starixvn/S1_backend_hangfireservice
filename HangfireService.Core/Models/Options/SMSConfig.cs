namespace HangfireService.Core.Models
{
    public class SmsWithOtpBaseConfig
    {
        public string SmsTpName { get; set; }
        public string AccountName { get; set; }
        public string GetBalanceUrl { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public int MiniMumLimit { get; set; }
        public string CheckUrl { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }

    public class NxCloudSmsConfig : SmsWithOtpBaseConfig { }

    public class SkyLineSmsConfig : SmsWithOtpBaseConfig { }

    public class SkyLineOtpConfig : SmsWithOtpBaseConfig { }

    public class AwsSmsConfig : SmsWithOtpBaseConfig { }


    public class LetsTalkConfig
    {
        public string SignInUrl { get; set; }
        public int SignInId { get; set; }
        public string SignInToken { get; set; }
        public string SandMessageUrl { get; set; }
        public string SandMessageRoomId { get; set; }
        public string SnsGroupName { get; set; }
        public string MonitoringGroupName { get; set; }
    }


    public class SKYLineSMSResult
    {
        public int status { get; set; }
        public string balance { get; set; }
        public string gift { get; set; }

        public string[][] array { get; set; }
    }

    public class LetsTalkSignResult
    {
        public int status { get; set; }
        public string message { get; set; }
        public string token { get; set; }

    }

    public class LetsTalkMsgParam
    {
        public string receiver { get; set; }
        public LetsTalkMsgtext msg { get; set; }

    }
    public class LetsTalkMsgtext
    {
        public string type { get; set; }
        public string text { get; set; }

    }


    public class NxCloudCheckResult
    {
        public string code { get; set; }
        public Row row { get; set; }
    }

    public class Row
    {
        public int billing_size { get; set; }
        public string date { get; set; }
        public string senderid { get; set; }
        public string phone { get; set; }
        public string dr_time { get; set; }
        public float billing_price { get; set; }
        public string msgid { get; set; }
        public string billing_status { get; set; }
        public string body { get; set; }
        public string dr_status { get; set; }
        public string dr { get; set; }
        public string network { get; set; }
    }

}
