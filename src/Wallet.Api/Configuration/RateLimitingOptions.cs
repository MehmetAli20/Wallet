using Wallet.Application.Abstractions.RateLimiting;

namespace Wallet.Api.Configuration
{
    public sealed class RateLimitPolicySettings
    {
        public int Capacity { get; set; }
        public int WindowSeconds { get; set; }
    }

    public sealed class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        public const string Anonymous = "anonymous-strict";
        public const string AnonymousRegister = "anonymous-register";
        public const string Authenticated = "authenticated";
        public const string AuthenticatedWrite = "authenticated-write";

        public bool Enabled { get; set; }

        public Dictionary<string, RateLimitPolicySettings> Policies { get; set; } = [];

        public RateLimitPolicy Resolve(string name)
        {
            if (!Policies.TryGetValue(name, out var settings))
            {
                throw new InvalidOperationException(
                    $"Rate limit policy '{name}' is not configured. Add RateLimiting:Policies:{name}.");
            }

            if (settings.Capacity <= 0 || settings.WindowSeconds <= 0)
            {
                throw new InvalidOperationException(
                    $"Rate limit policy '{name}' must have a positive Capacity and WindowSeconds.");
            }

            return new RateLimitPolicy(
                name, settings.Capacity, (double)settings.Capacity / settings.WindowSeconds);
        }

        public void EnsureUsable()
        {
            if (!Enabled)
            {
                return;
            }

            Resolve(Anonymous);
            Resolve(AnonymousRegister);
            Resolve(Authenticated);
            Resolve(AuthenticatedWrite);
        }
    }
}
