using Wallet.Application.Users;
using Wallet.Domain.Users;

namespace Wallet.Api.Contracts.Users
{
    public static class UserMappings
    {
        public static CurrentUserResponse ToCurrentUserResponse(this User user) =>
            new(user.Id, user.DisplayName);

        public static BlockedUserResponse ToBlockedUserResponse(this BlockedUser blocked) =>
            new(blocked.UserId, blocked.DisplayName);
    }
}
