using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HangfireService.Core.Extensions
{
    public static class MethodExtension
    {
        public static string GetCurrentMethod()
        {
            return new StackTrace().GetFrame(1)?.GetMethod()?.Name;
        }

        public static string GetCurrentAsyncMethod([CallerMemberName] string callerName = "")
        {
            return callerName;
        }
    }
}