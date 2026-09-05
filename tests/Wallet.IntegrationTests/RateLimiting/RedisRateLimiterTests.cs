using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Testcontainers.Redis;
using Wallet.Application.Abstractions.RateLimiting;
using Wallet.Infrastructure.RateLimiting;

namespace Wallet.IntegrationTests.RateLimiting
{
    public class RedisFixture : IAsyncLifetime
    {
        private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine").Build();

        public IConnectionMultiplexer Connection { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            Connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
        }

        public async Task DisposeAsync()
        {
            await Connection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }

    public class RedisRateLimiterTests : IClassFixture<RedisFixture>
    {
        private readonly RedisRateLimiter _limiter;

        public RedisRateLimiterTests(RedisFixture fixture) =>
            _limiter = new RedisRateLimiter(fixture.Connection, NullLogger<RedisRateLimiter>.Instance);

        private static RateLimitPolicy Login() => new("login", Capacity: 5, RefillPerSecond: 5d / 60);

        private static string NewKey() => Guid.NewGuid().ToString("N");

        [Fact]
        public async Task ItAllowsUpToCapacity_ThenDenies()
        {
            var key = NewKey();
            var policy = Login();

            for (var i = 0; i < 5; i++)
            {
                var allowed = await _limiter.TryAcquireAsync(key, policy);

                allowed.Allowed.Should().BeTrue($"request {i + 1} is within the capacity of 5");
                allowed.Remaining.Should().Be(4 - i);
            }

            var denied = await _limiter.TryAcquireAsync(key, policy);

            denied.Allowed.Should().BeFalse();
            denied.Remaining.Should().Be(0);
            denied.RetryAfterSeconds.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task TwoKeys_DoNotShareABucket()
        {
            var policy = Login();
            var alice = NewKey();
            var bob = NewKey();

            for (var i = 0; i < 5; i++)
                await _limiter.TryAcquireAsync(alice, policy);

            (await _limiter.TryAcquireAsync(alice, policy)).Allowed.Should().BeFalse();
            (await _limiter.TryAcquireAsync(bob, policy)).Allowed.Should().BeTrue();
        }

        [Fact]
        public async Task TheSameKeyUnderTwoPolicies_HasTwoBuckets()
        {
            var key = NewKey();
            var strict = new RateLimitPolicy("strict", Capacity: 1, RefillPerSecond: 0.1);
            var relaxed = new RateLimitPolicy("relaxed", Capacity: 5, RefillPerSecond: 0.1);

            (await _limiter.TryAcquireAsync(key, strict)).Allowed.Should().BeTrue();
            (await _limiter.TryAcquireAsync(key, strict)).Allowed.Should().BeFalse();

            (await _limiter.TryAcquireAsync(key, relaxed)).Allowed.Should().BeTrue();
        }

        [Fact]
        public async Task TokensComeBackOverTime()
        {
            var key = NewKey();
            var fast = new RateLimitPolicy("fast", Capacity: 2, RefillPerSecond: 20);

            (await _limiter.TryAcquireAsync(key, fast)).Allowed.Should().BeTrue();
            (await _limiter.TryAcquireAsync(key, fast)).Allowed.Should().BeTrue();
            (await _limiter.TryAcquireAsync(key, fast)).Allowed.Should().BeFalse();

            await Task.Delay(TimeSpan.FromMilliseconds(300));

            (await _limiter.TryAcquireAsync(key, fast)).Allowed.Should().BeTrue();
        }

        [Fact]
        public async Task RefillNeverExceedsCapacity()
        {
            var key = NewKey();
            var fast = new RateLimitPolicy("cap", Capacity: 2, RefillPerSecond: 50);

            await _limiter.TryAcquireAsync(key, fast);

            await Task.Delay(TimeSpan.FromMilliseconds(500));

            var first = await _limiter.TryAcquireAsync(key, fast);
            var second = await _limiter.TryAcquireAsync(key, fast);
            var third = await _limiter.TryAcquireAsync(key, fast);

            first.Allowed.Should().BeTrue();
            second.Allowed.Should().BeTrue();
            third.Allowed.Should().BeFalse();
        }

        [Fact]
        public async Task WhenRedisIsUnreachable_ItFailsOpen()
        {
            var options = ConfigurationOptions.Parse("127.0.0.1:6399");
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 200;

            await using var dead = await ConnectionMultiplexer.ConnectAsync(options);

            var limiter = new RedisRateLimiter(dead, NullLogger<RedisRateLimiter>.Instance);

            var decision = await limiter.TryAcquireAsync(NewKey(), Login());

            decision.Allowed.Should().BeTrue();
            decision.Remaining.Should().Be(-1);
        }
    }
}
