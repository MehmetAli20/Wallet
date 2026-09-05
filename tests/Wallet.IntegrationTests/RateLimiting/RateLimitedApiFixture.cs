using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Wallet.Api.Contracts.Users;
using Wallet.Infrastructure.Persistence;

namespace Wallet.IntegrationTests.RateLimiting
{
    public abstract class RateLimitedApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
        private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

        protected abstract int AnonymousCapacity { get; }
        protected abstract int AuthenticatedCapacity { get; }
        protected abstract int WriteCapacity { get; }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            await using var context = new WalletDbContext(options, TestCurrentUser.System);
            await context.Database.MigrateAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:WalletDb", _postgres.GetConnectionString());
            builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
            builder.UseSetting("Jwt:SigningKey", "wallet-integration-tests-signing-key-32b+");
            builder.UseSetting("Jwt:Issuer", "wallet-tests");
            builder.UseSetting("Jwt:Audience", "wallet-tests");
            builder.UseSetting("RateLimiting:Enabled", "true");

            Capacity(builder, "anonymous-strict", AnonymousCapacity);
            Capacity(builder, "authenticated", AuthenticatedCapacity);
            Capacity(builder, "authenticated-write", WriteCapacity);
        }

        private static void Capacity(IWebHostBuilder builder, string policy, int capacity)
        {
            builder.UseSetting($"RateLimiting:Policies:{policy}:Capacity", capacity.ToString());
            builder.UseSetting($"RateLimiting:Policies:{policy}:RefillPerMinute", "1");
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _postgres.DisposeAsync();
            await _redis.DisposeAsync();
            await base.DisposeAsync();
        }

        public async Task<HttpClient> RegisterAsync()
        {
            var client = CreateClient();
            var username = $"u{Guid.NewGuid():N}"[..20];

            await client.PostAsJsonAsync("/api/v1/auth/register",
                new RegisterRequest(username, $"{username}@test.com", "password123"));

            var login = await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(username, "password123"));

            var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }
    }

    public sealed class WriteLimitFixture : RateLimitedApiFixture
    {
        protected override int AnonymousCapacity => 200;
        protected override int AuthenticatedCapacity => 200;
        protected override int WriteCapacity => 3;
    }

    public sealed class AnonymousLimitFixture : RateLimitedApiFixture
    {
        protected override int AnonymousCapacity => 3;
        protected override int AuthenticatedCapacity => 200;
        protected override int WriteCapacity => 200;
    }
}
