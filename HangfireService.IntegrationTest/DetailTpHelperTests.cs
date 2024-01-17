using Dapper;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;
using HangfireService.Core.ThirdParty.Interfaces;
using HangfireService.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NSubstitute;

using NUnit.Framework;

namespace HangfireService.IntegrationTest
{
    public class DetailTpHelperTests
    {
        private readonly IServiceProvider _provider;

        private IDetailTpHelper _tpHelper;

        private readonly MySqlClient _dbClient;

        public DetailTpHelperTests()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var logger = Substitute.For<ILogger<MySqlClient>>();
            builder.Services.AddTransient(p => logger);
            builder.Services
                .AddDatabaseConnection(builder.Configuration)
                .RegisterServices()
                .AddHangfireService()
                .RegisterScheduledJobs()
                .RegisterTpHelper();

            var app = builder.Build();

            _dbClient = app.Services.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _provider = app.Services;
        }

        [SetUp]
        public void SetUp()
        {
            _tpHelper = null;
        }

        [TestCase(1, 1, "113608583470", 1, 2, "DNova88_1170", 1170, "tottitest3")]
        public void GetTpBetRecordDetailTest(int tpId, int gameType, string transId, int siteId, int currencyId, string tpUserName, int custId, string userName)
        {
            _tpHelper = _provider.GetServices<IDetailTpHelper>().First(p => p.TpId == tpId);
            var todoList = new List<TpDetailTodo>()
            {
                new TpDetailTodo
                {
                    TpId = tpId,
                    GameType = gameType,
                    TransId = transId,
                    SiteId = siteId,
                    CurrencyId = currencyId,
                    TpUserName = tpUserName,
                    CustId = custId,
                    UserName = userName,
                    SettledTime = DateTime.UtcNow
                }
            };

            foreach (var todo in todoList)
            {
                _dbClient.Query<int>("Service_TpDetailTodo_Ins", parameters: new DynamicParameters(todo));
            }

            var resultList = _tpHelper.GetTpBetRecordDetailAsync(todoList, new TpDetailOption() { TpId = tpId })
                .ConfigureAwait(false).GetAwaiter().GetResult();
            var alltrue = resultList.ToList().TrueForAll(b => b);
            Assert.IsTrue(alltrue);
        }

    }
}