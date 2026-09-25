namespace Wallet.Application.Abstractions.RateLimiting
{
    public interface IRateLimiter
    {
        Task<RateLimitDecision> TryAcquireAsync(
            string key, RateLimitPolicy policy, CancellationToken cancellationToken = default);
    }
}
