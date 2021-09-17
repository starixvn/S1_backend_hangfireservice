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
    public class PGTpHelperTests
    {
        private readonly ITpHelper _tpHelper;

        private readonly IConfiguration _configuration;

        private readonly MySqlClient _dbClient;

        public PGTpHelperTests()
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
            var provider = services.BuildServiceProvider();

            _tpHelper = provider.GetServices<ITpHelper>().First(p => p.TpId == 14);
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        [Test]
        public void GetTpBetRecordDetailTest()
        {
            var todoList = new List<TpDetailTodo>();
            var TransIdList = new List<string>
            {
                "1428627374543126528",
                "1428627363730214912",
                "1428627360982941696",
                "1428627299976794112",
                "1428627294218010624",
            };
            
            foreach (var transId in TransIdList)
            {
                var todo = new TpDetailTodo
                {
                    TpId = 14,
                    GameType = 17,
                    TransId = transId,
                    SiteId = 1,
                    CurrencyId = 2,
                    TpUserName = "test",
                    CustId = 1,
                    UserName = "test",
                };
                
                _dbClient.Query<int>("Service_TpDetailTodo_Ins", parameters: new DynamicParameters(todo));
                todoList.Add(todo);
            }

            var resultList = _tpHelper.GetTpBetRecordDetailAsync(todoList).ConfigureAwait(false).GetAwaiter().GetResult();
            var alltrue = resultList.ToList().TrueForAll(b => b);
            Assert.IsTrue(alltrue);
        }

    }
}