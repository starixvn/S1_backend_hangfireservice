using System.Dynamic;

namespace HangfireService.Core.Models.Jobs
{
    public class JobOption
    {
        public string Type { get; set; }

        public string ClassName { get; set; }

        public string JobName { get; set; }

        public string Description { get; set; }

        public string Cron { get; set; }

        public bool Enable { get; set; }

        public ExpandoObject Parameters { get; set; }
    }
}
