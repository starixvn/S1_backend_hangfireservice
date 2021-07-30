using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace HangfireService.Core.Models.ThirdParty.Saba
{

    public class GetBetDetailByTransIDResponse
    {
        public string error_code { get; set; }
        public string message { get; set; }
        public dynamic Data { get; set; }
    }
}
