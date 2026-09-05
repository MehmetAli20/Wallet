using System.Reflection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wallet.Application.Abstractions.RateLimiting;

namespace Wallet.Infrastructure.RateLimiting
{
    public class RedisRateLimiter : IRateLimiter
    {
        private static readonly string Script = LoadScript();

        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisRateLimiter> _logger;

        public RedisRateLimiter(IConnectionMultiplexer redis, ILogger<RedisRateLimiter> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<RateLimitDecision> TryAcquireAsync(
            string key, RateLimitPolicy policy, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = (RedisValue[])(await _redis.GetDatabase().ScriptEvaluateAsync(
                    Script,
                    [$"rl:{policy.Name}:{key}"],
                    [policy.Capacity, policy.RefillPerSecond, 1, policy.TtlSeconds]))!;

                return new RateLimitDecision(
                    Allowed: (int)result[0] == 1,
                    Remaining: (int)result[1],
                    RetryAfterSeconds: (int)result[2]);
            }
            catch (Exception exception) when (
                exception is RedisException or RedisTimeoutException or ObjectDisposedException)
            {
                _logger.LogError(
                    exception,
                    "Rate limiting is unavailable; allowing {Policy} for {Key} unchecked.",
                    policy.Name,
                    key);

                return RateLimitDecision.Unavailable;
            }
        }

        private static string LoadScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames()
                .Single(n => n.EndsWith("TokenBucket.lua", StringComparison.Ordinal));

            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}