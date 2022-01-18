namespace HangfireService.Core.Models.DBs
{

    public class LicenseReport
    {
        public string Data { get; set; }
    }

    public class CuracaoResponse
    {
        public int status { get; set; }
        public string message { get; set; }
    }

    public class JobResult
    {
        public int UploadS3Success { get; set; }
        public int UploadS3Fail { get; set; }
        public int UploadSuccess { get; set; }
        public int UploadFail { get; set; }
    }
}