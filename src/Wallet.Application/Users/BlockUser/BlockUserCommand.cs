using MediatR;

namespace Wallet.Application.Users.BlockUser
{
    public record BlockUserCommand(Guid UserId) : IRequest;
}
