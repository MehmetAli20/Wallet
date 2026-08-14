using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Users;

namespace Wallet.IntegrationTests
{
    public class TestCurrentUser : ICurrentUser
    {

        public Guid UserId { get; }
        public bool IsSystem { get; }

        private TestCurrentUser(Guid userId, bool isSystem)
        {
            UserId = userId;
            IsSystem = isSystem;
        }

        public static TestCurrentUser For(Guid userId)
        {
            return new(userId, isSystem: false);
        }

        public static TestCurrentUser System { get; } = new(Guid.Empty, isSystem: true);
    }
}
