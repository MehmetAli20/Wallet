using MediatR;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.RenameCurrentUser
{
    public record RenameCurrentUserCommand(string DisplayName) : IRequest<User>;
}
