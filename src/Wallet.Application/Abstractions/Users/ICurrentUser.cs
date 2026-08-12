using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions.Users
{
    public interface ICurrentUser
    {
        Guid UserId { get; }
        bool IsSystem { get; }
    }
}
