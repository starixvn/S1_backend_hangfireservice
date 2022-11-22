namespace HangfireService.Core.Models.Options
{
    public class SMSOption
    {
        public string SMSTPName { get; set; }
        public string Accountname { get; set; }
        public string URL { get; set; }
        public string KEY1 { get; set; }
        public string KEY2 { get; set; }
        public string KEY3 { get; set; }
        public int Minimumlimit { get; set; }
        public string CheckURL { get; set; }
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
