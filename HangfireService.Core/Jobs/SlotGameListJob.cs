using System;
using System.ComponentModel;
using System.Security.Policy;

using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using Hangfire.Storage.Monitoring;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;

using Microsoft.Extensions.Logging;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("SlotGameListJob", queue: "default")]
    public class SlotGameListJob : BaseJob, IScheduledJob
    {
        private readonly MySqlClient _reportDbClient;
        private static List<JobOption> _jobList;
        private readonly int _isStage;
        private readonly ICacheHelper _redisHelper;
        private readonly IEnumerable<ServerInfo> _serverInfoList;

        public SlotGameListJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("default", out _jobList);
           
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _isStage = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<int>("IsStage");
            _serverInfoList = provider.GetRequiredService<IEnumerable<ServerInfo>>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            
            var deviceTypeList = JsonConvert.DeserializeObject<List<int>>(jobOption.Parameters);
            var totalCount = 0;
            foreach (var serverInfo in _serverInfoList)
            {
                foreach (var currencyId in serverInfo.ICurrencyList)
                {
                    foreach (var deviceType in deviceTypeList)
                    {
                        totalCount += await SetSlotGameList(serverInfo.SiteId, currencyId, deviceType);
                    }
                }               
            }
            
            return totalCount;
        }

        private async Task<int> SetSlotGameList(int siteId, int currencyId, int deviceType)
        {
            var statusList = new List<int> { 1 };
            if (_isStage == 1)
            {
                statusList.Add(3);
            }
           
            var result = await _reportDbClient.QueryAsync<SlotGameInfo>("Service_SlotGame_Get", new
            {
                CurrencyId = currencyId,
                SiteId = siteId,
                DeviceType = deviceType,
                StatusJson = JsonConvert.SerializeObject(statusList)
            });
            var key = $"GetSlotGameListV2:{_isStage}:{currencyId}:{siteId}:{deviceType}";
            _redisHelper.StringSet(key, JsonConvert.SerializeObject(result), null);

            return result.Count();
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);


            var result = ExecuteAsync($"Execute.{jobName}", jobOption, context, cancellationToken).Result;


            return result;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("SlotGameListJob"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
