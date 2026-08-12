using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
