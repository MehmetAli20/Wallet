using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Wallet.Infrastructure.Persistence;

namespace Wallet.IntegrationTests.Api
{
    public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options;

            await using var context = new WalletDbContext(options, TestCurrentUser.System);
            await context.Database.MigrateAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:WalletDb", _container.GetConnectionString());
            builder.UseSetting("Jwt:SigningKey", "wallet-integration-tests-signing-key-32b+");
            builder.UseSetting("Jwt:Issuer", "wallet-tests");
            builder.UseSetting("Jwt:Audience", "wallet-tests");
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _container.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
