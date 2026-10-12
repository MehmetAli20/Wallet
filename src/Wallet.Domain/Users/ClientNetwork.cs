using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Wallet.Domain.Users
{
    public static class ClientNetwork
    {
        public static string? Key(string? clientIp)
        {
            if (!IPAddress.TryParse(clientIp, out var address))
            {
                return null;
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

            return $"{new IPAddress(bytes)}/64";
        }

        public static bool IsFamiliar(string? clientIp, IEnumerable<string> knownClientIps)
        {
            var key = Key(clientIp);

            return key is not null && knownClientIps.Any(known => Key(known) == key);
        }
    }
}
