using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Users
{
    public class ContactPolicy
    {
        private readonly IUserRepository _users;
        private readonly IUserBlockRepository _blocks;
        private readonly ICurrentUser _currentUser;

        public ContactPolicy(IUserRepository users, IUserBlockRepository blocks, ICurrentUser currentUser)
        {
            _users = users;
            _blocks = blocks;
            _currentUser = currentUser;
        }

        public async Task EnsureCanReachAsync(Guid userId, CancellationToken cancellationToken)
        {
            var other = await _users.GetByIdAsync(userId, cancellationToken);

            if (other is null
                || other.IsDeleted
                || await _blocks.HasBlockedAsync(userId, _currentUser.UserId, cancellationToken))
                throw new InvalidGroupOperationException("User not found.");

            if (await _blocks.HasBlockedAsync(_currentUser.UserId, userId, cancellationToken))
                throw new InvalidGroupOperationException("You have blocked this user. Unblock them first.");
        }
    }
}
