using System;
using System.Collections.Generic;
using System.Text;

namespace HangfireService.Core.Models.Jobs
{
    public class JobDetailOption
    {
        public string Type { get; set; }

        public string Name { get; set; }

        public string Group { get; set; }

        public string Description { get; set; }

        public string Cron { get; set; }

        public bool Enable { get; set; }
    }
}
