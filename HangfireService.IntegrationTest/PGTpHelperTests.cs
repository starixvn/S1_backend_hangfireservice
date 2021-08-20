using FluentAssertions;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.ThirdParty;
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
    public class Tests
    {
        private PGTpHelper _tpHelper;

        private IConfiguration _configuration;

        [SetUp]
        public void Setup()
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

            _tpHelper = new PGTpHelper(provider);
        }

        [Test]
        public void GetTpBetRecordDetailTest()
        {
            var todoList = new List<TpDetailTodo>
            {
                {
                    new TpDetailTodo
                    {
                        TpId=14,
                        GameType =17,
                        TransId ="1428627374543126528",
                    }
                },
                {
                    new TpDetailTodo
                    {
                        TpId=14,
                        GameType =17,
                        TransId ="1428627363730214912",
                    }
                },
                {
                    new TpDetailTodo
                    {
                        TpId=14,
                        GameType =17,
                        TransId ="1428627360982941696",
                    }
                },
                {
                    new TpDetailTodo
                    {
                        TpId=14,
                        GameType =17,
                        TransId ="1428627299976794112",
                    }
                },
                {
                    new TpDetailTodo
                    {
                        TpId=14,
                        GameType =17,
                        TransId ="1428627294218010624",
                    }
                },
            };
            var result = _tpHelper.GetTpBetRecordDetailAsync(todoList).ConfigureAwait(false).GetAwaiter().GetResult();
            var actual = result.Where(item => item);
            actual.Count().Should().Be(todoList.Count);
        }

    }
}