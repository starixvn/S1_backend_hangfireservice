namespace HangfireService.Core.Models.Options
{
    public class TpDetailOption
    {
        public int TpId { get; set; }
        public int LiminNum { get; set; } = 1000;
        public int TaskCount { get; set; } = 2;
    }
}
