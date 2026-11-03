using MediatR;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Users.GetBlockedUsers
{
    public class GetBlockedUsersQueryHandler : IRequestHandler<GetBlockedUsersQuery, IReadOnlyList<BlockedUser>>
    {
        private readonly IUserBlockRepository _blocks;
        private readonly ICurrentUser _currentUser;

        public GetBlockedUsersQueryHandler(IUserBlockRepository blocks, ICurrentUser currentUser)
        {
            _blocks = blocks;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<BlockedUser>> Handle(
            GetBlockedUsersQuery request, CancellationToken cancellationToken) =>
            await _blocks.GetBlockedByAsync(_currentUser.UserId, cancellationToken);
    }
}
