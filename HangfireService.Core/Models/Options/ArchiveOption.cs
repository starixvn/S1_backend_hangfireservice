namespace HangfireService.Core.Models.Options
{
    public class ArchiveOption
    {
        public int LiminNum { get; set; } = 1000;
        public int TaskCount { get; set; } = 2;
        public string GetSP { get; set; } = string.Empty;
        public string InsertSP { get; set; } = string.Empty;
        public string DeleteSP { get; set; } = string.Empty;
    }
}
