using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Infrastructure.Authentication
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string SigningKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 15;
    }
}
