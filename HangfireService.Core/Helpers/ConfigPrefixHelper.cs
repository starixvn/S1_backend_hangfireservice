namespace HangfireService.Core.Helpers
{
    public class ConfigPrefixHelper
    {
        private readonly string _cloudPrefixKey = "CloudPrefix";
        private readonly string _cloudPrefixValue = "aws";

        public string ProxySetting =>
            Environment.GetEnvironmentVariable(_cloudPrefixKey) == _cloudPrefixValue
            ? "awsProxySetting"
            : "ProxySetting";


        public string ServiceHost =>
            Environment.GetEnvironmentVariable(_cloudPrefixKey) == _cloudPrefixValue
                ? "awsServiceHost"
                : "ServiceHost";
    }
}
