namespace Wallet.Application.Abstractions.RateLimiting
{
    public record RateLimitPolicy(string Name, int Capacity, double RefillPerSecond)
    {
        public int TtlSeconds => (int)Math.Ceiling(Capacity / RefillPerSecond) * 2;
    }
}
