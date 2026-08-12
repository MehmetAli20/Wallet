using Wallet.Application.Abstractions.Users;

namespace Wallet.Worker.Authentication
{
    public class SystemCurrentUser : ICurrentUser
    {
        public bool IsSystem => true;
        public Guid UserId => Guid.Empty;
    }
}
