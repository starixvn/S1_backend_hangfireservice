using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.Service
{
    public class RegularService
    {
        private readonly HttpHelper _httpHelper;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ILogger<FPSService> _logger;
        private readonly MainDBDataLayer _mainDBDataLayer;
        private readonly ReportDBDataLayer _reportDBDataLayer;
        
        public RegularService(IServiceProvider provider)
        {
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<FPSService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public async Task<int> SetCustFPSTimeUpdate()
        {
            var nowString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var nowMinus15String = DateTime.Now.AddMinutes(-15).ToString("yyyy-MM-dd HH:mm:ss");
            var replaceResult = _mainDBDataLayer.SetCustFPSReplaceAccumulation(nowMinus15String,nowString);
            return replaceResult;
        }
        
        
        /// <summary>
        /// 將 JSON 字串反序列化為指定類型的對象。
        /// </summary>
        private static T RegularDeserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Json Is Null Or WhiteSpace", nameof(json));
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("JSON Fail", ex);
            }
        }
        
    }
}