namespace Wallet.Application.Abstractions.RateLimiting
{
    public record RateLimitDecision(bool Allowed, int Remaining, int RetryAfterSeconds)
    {
        public static RateLimitDecision Allow(int remaining) => new(true, remaining, 0);

        public static readonly RateLimitDecision Unavailable = new(true, -1, 0);
    }
}
