using System;
using System.Security.Cryptography;
using System.Text;

namespace Wallet.Domain.Common
{
    public static class SecureToken
    {
        private const int TokenBytes = 32;
        private const int BodyLength = (TokenBytes * 4 + 2) / 3;

        public static string New(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

            var body = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return $"{prefix}_{body}";
        }

        public static bool HasFormat(string? token, string prefix)
        {
            if (token is null || token.Length != prefix.Length + 1 + BodyLength)
                return false;

            if (!token.StartsWith(prefix, StringComparison.Ordinal) || token[prefix.Length] != '_')
                return false;

            foreach (var c in token.AsSpan(prefix.Length + 1))
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')
                    return false;
            }

            return true;
        }

        public static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }
}
