using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Extensions
{
    public static class ExpandoObjectExtenions
    {
        public static ExpandoObject Update(this ExpandoObject expandoObject, string key, string value)
        {
            var parameters = expandoObject as IDictionary<string, object>;
            parameters.Remove(key);
            parameters.Add(key, value);
            return (ExpandoObject)parameters;
        }
    }
}
