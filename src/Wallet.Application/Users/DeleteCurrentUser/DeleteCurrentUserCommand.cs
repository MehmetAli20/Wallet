using MediatR;

namespace Wallet.Application.Users.DeleteCurrentUser
{
    public record DeleteCurrentUserCommand(string Password) : IRequest
    {
        public sealed override string ToString() =>
            $"{nameof(DeleteCurrentUserCommand)} {{ Password = *** }}";
    }
}
