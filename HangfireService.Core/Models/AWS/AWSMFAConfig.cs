namespace HangfireService.Core.Models.AWS;

public class AWSMFAConfig
{
    public string AccessKey { get; set; }
    public string SecretAccessKey { get; set; }
    public string SecretKey { get; set; }
    public string SerialNumber { get; set; }
    public int DurationSeconds { get; set; }
}