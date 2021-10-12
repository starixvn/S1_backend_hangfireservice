using Dapper;
using FluentAssertions;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.ThirdParty;
using HangfireService.Core.ThirdParty.Interfaces;
using HangfireService.IntegrationTest.Stubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HangfireService.IntegrationTest
{
    public class TpHelperTests
    {
        private readonly IConfiguration _configuration;

        private readonly ServiceProvider _provider;

        private ITpHelper _tpHelper;

        public TpHelperTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IHostEnvironment>(new HostingEnvironment()
            {
                EnvironmentName = Environments.Development,
            });

            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .AddJsonFile("appsettings.Local.json", false, false);
            _configuration = configBuilder.Build();
            services.AddSingleton(_configuration);

            var logger = Substitute.For<ILogger<MySqlClient>>();
            services.AddTransient(p => logger);

            new StubStartUp(_configuration).ConfigureServices(services);
            _provider = services.BuildServiceProvider();

            _tpHelper = _provider.GetServices<ITpHelper>().First(p => p.TpId == 14);
        }

        [SetUp]
        public void SetUp()
        {
            _tpHelper = null;
        }

        [TestCase(7, 12, "202109281317528230506732100012", 1, 4, "dnova88x1360", 1360, "alicethb")]
        public void GetTpBetRecordDetailTest(int tpId, int gameType, string transId, int siteId, int currencyId, string tpUserName, int custId, string userName)
        {
            _tpHelper = _provider.GetServices<ITpHelper>().First(p => p.TpId == tpId);
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
                }
            };
            var resultList = _tpHelper.GetTpBetRecordDetailAsync(todoList).ConfigureAwait(false).GetAwaiter().GetResult();
            var alltrue = resultList.ToList().TrueForAll(b => b);
            Assert.IsTrue(alltrue);
        }

    }
}