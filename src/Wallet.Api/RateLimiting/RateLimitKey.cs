using System.Net;
using System.Net.Sockets;
using System.Security.Claims;

namespace Wallet.Api.RateLimiting
{
    public static class RateLimitKey
    {
        public static string For(HttpContext context)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return string.IsNullOrEmpty(userId)
                ? $"ip:{Normalize(context.Connection.RemoteIpAddress)}"
                : $"u:{userId}";
        }

        private static string Normalize(IPAddress? address)
        {
            if (address is null)
            {
                return "unknown";
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return address.ToString();
            }

            var bytes = address.GetAddressBytes();
            Array.Clear(bytes, 8, 8);

            return new IPAddress(bytes).ToString();
        }
    }
}
