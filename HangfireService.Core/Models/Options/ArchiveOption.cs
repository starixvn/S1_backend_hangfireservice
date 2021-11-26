namespace HangfireService.Core.Models.Options
{
    public class ArchiveOption
    {
        public int LiminNum { get; set; } = 100;
        public int TaskCount { get; set; } = 2;
        /// <summary>
        /// 暫停小時
        /// </summary>
        public int PauseHours { get; set; } = 12;
        public string GetSP { get; set; } = string.Empty;
        public string InsertSP { get; set; } = string.Empty;
        public string DeleteSP { get; set; } = string.Empty;
    }
}
