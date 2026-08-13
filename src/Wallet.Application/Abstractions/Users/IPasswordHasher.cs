using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions.Users
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string passwordHash);
    }
}
