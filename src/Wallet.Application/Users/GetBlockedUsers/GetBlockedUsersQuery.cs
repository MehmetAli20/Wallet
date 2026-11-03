using MediatR;

namespace Wallet.Application.Users.GetBlockedUsers
{
    public record GetBlockedUsersQuery : IRequest<IReadOnlyList<BlockedUser>>;
}
