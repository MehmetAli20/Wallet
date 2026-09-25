using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Wallet.Api.Contracts.Users;
using Wallet.Infrastructure.Persistence;

namespace Wallet.IntegrationTests.Api
{
    public sealed record TestUser(Guid Id, HttpClient Client);

    public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string DisplayName = "Test User";

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
            builder.UseSetting("RateLimiting:Enabled", "false");
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _container.DisposeAsync();
            await base.DisposeAsync();
        }

        public async Task<TestUser> RegisterAsync()
        {
            var client = CreateClient();
            var username = $"u{Guid.NewGuid():N}"[..20];

            var register = await client.PostAsJsonAsync("/api/v1/auth/register",
                new RegisterRequest(username, $"{username}@test.com", "password123", DisplayName));
            register.StatusCode.Should().Be(HttpStatusCode.Created);
            var userId = (await register.Content.ReadFromJsonAsync<RegisterResponse>())!.UserId;

            var login = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(username, "password123"));
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return new TestUser(userId, client);
        }
    }
}
