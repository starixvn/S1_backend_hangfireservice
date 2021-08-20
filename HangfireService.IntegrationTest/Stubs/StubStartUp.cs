using Microsoft.Extensions.Configuration;


namespace HangfireService.IntegrationTest.Stubs
{
    public class StubStartUp : Startup
    {
        public StubStartUp(IConfiguration configuration) : base(configuration) { }
    }
}
