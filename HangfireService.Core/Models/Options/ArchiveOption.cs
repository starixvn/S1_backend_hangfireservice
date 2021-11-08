namespace HangfireService.Core.Models.Options
{

    public class ArchiveOption
    { 
        public int LiminNum { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int ColdDownTime { get; set; }
        public string GetSP { get; set; }
        public string InsertSP { get; set; }
        public string DeleteSP { get; set; }
    }
}
