using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Wallet.Api.Configuration
{
    public class ForwardingOptions
    {
        public const string SectionName = "Forwarding";

        public bool Enabled { get; set; }
        public int ForwardLimit { get; set; } = 1;
        public string[] KnownProxies { get; set; } = [];
        public string[] KnownNetworks { get; set; } = [];

        public void ApplyTo(ForwardedHeadersOptions options)
        {
            if (Enabled && KnownProxies.Length == 0 && KnownNetworks.Length == 0)
            {
                throw new InvalidOperationException(
                    "Forwarding is enabled but no proxy is trusted. ASP.NET skips the trust check " +
                    "when both lists are empty, which would let any caller spoof X-Forwarded-For. " +
                    "Set Forwarding:KnownNetworks or Forwarding:KnownProxies, or disable forwarding.");
            }

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = ForwardLimit;

            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var proxy in KnownProxies)
                options.KnownProxies.Add(IPAddress.Parse(proxy));

            foreach (var network in KnownNetworks)
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    }
}
