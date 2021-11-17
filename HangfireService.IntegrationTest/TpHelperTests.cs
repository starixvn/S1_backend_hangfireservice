using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.ThirdParty.Interfaces;
using HangfireService.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace HangfireService.IntegrationTest
{
    public class TpHelperTests
    {
        private readonly IConfiguration _configuration;

        private readonly ServiceProvider _provider;

        private ITpHelper? _tpHelper;

        private readonly MySqlClient _dbClient;

        public TpHelperTests()
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddSingleton<IHostEnvironment>(new HostingEnvironment()
            {
                EnvironmentName = Environments.Development,
            });

            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .AddJsonFile("appsettings.Local.json", false, false);
            _configuration = configBuilder.Build();
            builder.Services.AddSingleton(_configuration);

            var logger = Substitute.For<ILogger<MySqlClient>>();
            builder.Services.AddTransient(p => logger);
            builder.Services
                .AddDatabaseConnection(builder.Configuration)
                .RegisterServices(builder.Configuration)
                .AddHangfireService(builder.Configuration)
                .RegisterScheduledJobs(builder.Configuration)
                .RegisterTpHelper();

            var app = builder.Build();
       
            _dbClient = app.Services.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _provider = (ServiceProvider)app.Services;
        }

        [SetUp]
        public void SetUp()
        {
            _tpHelper = null;
        }

        [TestCase(1, 1, "113608583470", 1, 2, "DNova88_1170", 1170, "tottitest3")]
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
                    SettledTime = DateTime.UtcNow
                }
            };

            foreach (var todo in todoList)
            {
                _dbClient.Query<int>("Service_TpDetailTodo_Ins", parameters: new DynamicParameters(todo));
            }
            
            var resultList = _tpHelper.GetTpBetRecordDetailAsync(todoList).ConfigureAwait(false).GetAwaiter().GetResult();
            var alltrue = resultList.ToList().TrueForAll(b => b);
            Assert.IsTrue(alltrue);
        }

    }
}