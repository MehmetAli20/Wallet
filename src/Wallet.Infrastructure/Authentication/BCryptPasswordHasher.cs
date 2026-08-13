using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Infrastructure.Authentication
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;

        private static readonly string _dummyHash =
            BCrypt.Net.BCrypt.HashPassword("timing-equalization-dummy", WorkFactor);

        public string DummyHash => _dummyHash;

        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool Verify(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}