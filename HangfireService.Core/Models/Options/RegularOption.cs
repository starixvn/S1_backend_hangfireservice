using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Models.Options
{
    public class RegularOption
    {
        public string SpName { get; set; }
        public string SpParameters { get; set; } = null;
    }
}
