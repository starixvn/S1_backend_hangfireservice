namespace HangfireService.Core.Models.AWS
{
    internal class AWSS3CredebtialOption
    {
        public int Type { get; set; } = 0;// 0=check, 1=update
        public int KeyId { get; set; }
    }
}
