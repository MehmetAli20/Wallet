using MediatR;

namespace Wallet.Application.Users.UnblockUser
{
    public record UnblockUserCommand(Guid UserId) : IRequest;
}
