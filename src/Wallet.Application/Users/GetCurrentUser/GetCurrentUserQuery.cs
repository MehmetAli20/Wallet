using MediatR;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.GetCurrentUser
{
    public record GetCurrentUserQuery : IRequest<User>;
}
