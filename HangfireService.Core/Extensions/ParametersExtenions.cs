using Newtonsoft.Json.Linq;

namespace HangfireService.Core.Extensions
{
    public static class parametersExtenions
    {
        public static string Update(this string parameters, string key, string value)
        {
            var jobject = JObject.Parse(parameters);
            jobject[key] = value;
            return jobject.ToString();
        }
    }
}
