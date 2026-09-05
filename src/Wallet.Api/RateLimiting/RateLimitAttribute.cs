namespace Wallet.Api.RateLimiting
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RateLimitAttribute : Attribute
    {
        public RateLimitAttribute(string policy) => Policy = policy;

        public string Policy { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class NoRateLimitAttribute : Attribute
    {
    }
}
