using HangfireService.Core.DataLayers;
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
    public class FinanceTpHelperTests
    {
        private readonly IServiceProvider _provider;

        private IFinanceTpHelper _tpHelper;

        private readonly MySqlClient _dbClient;

        public FinanceTpHelperTests()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });

            var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
            //remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, new[] { "Nova" }, remoteUrls);
            builder.Configuration.AddRemoteJsonConfig(remoteUrls);

            var logger = Substitute.For<ILogger<MySqlClient>>();
            builder.Services.AddTransient(p => logger);
            builder.Services
                .AddDatabaseConnection(builder.Configuration)
                .RegisterServices()
                .AddHangfireService()
                .RegisterScheduledJobs()
                .RegisterTpHelper()
                .AddOption();

            var app = builder.Build();

            _dbClient = app.Services.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _provider = app.Services;
        }

        [SetUp]
        public void SetUp()
        {
            _tpHelper = null;
        }

        [TestCase(27)]
        public void GetBettransCheckDataTest(int tpId)
        {
            _tpHelper = _provider.GetServices<IFinanceTpHelper>().First(p => p.TpId == tpId);
            var resultList = _tpHelper.GetBettransCheckData(new FinanceCheckOption()).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.IsTrue(resultList.Any());
        }
    }
}