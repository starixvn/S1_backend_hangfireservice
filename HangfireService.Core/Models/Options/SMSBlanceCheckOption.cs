namespace HangfireService.Core.Models.Options
{
    public class SMSBlanceCheckOption
    {
        public string SMSTPName { get; set; }
        public string Accountname { get; set; }
        public string URL { get; set; }
        public string KEY1 { get; set; }
        public string KEY2 { get; set; }
        public string KEY3 { get; set; }
        public int Minimumlimit { get; set; }
    }

    public class SKYLineSMSResult
    {
        public int status { get; set; }
        public string balance { get; set; }
        public string gift { get; set; }

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
}
