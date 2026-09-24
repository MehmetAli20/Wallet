using MediatR;

namespace Wallet.Application.Users.Register
{
    public record RegisterCommand(string Username, string Email, string Password) : IRequest<Guid>
    {
        public sealed override string ToString() =>
            $"{nameof(RegisterCommand)} {{ Username = {Username}, Email = ***, Password = *** }}";
    }
}
