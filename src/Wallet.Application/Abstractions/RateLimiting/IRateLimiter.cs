namespace Wallet.Application.Abstractions.RateLimiting
{
    public record RateLimitPolicy(string Name, int Capacity, double RefillPerSecond)
    {
        public int TtlSeconds => (int)Math.Ceiling(Capacity / RefillPerSecond) * 2;
    }

    public record RateLimitDecision(bool Allowed, int Remaining, int RetryAfterSeconds)
    {
        public static RateLimitDecision Allow(int remaining) => new(true, remaining, 0);

        public static readonly RateLimitDecision Unavailable = new(true, -1, 0);
    }

    public interface IRateLimiter
    {
        Task<RateLimitDecision> TryAcquireAsync(
            string key, RateLimitPolicy policy, CancellationToken cancellationToken = default);
    }
}
