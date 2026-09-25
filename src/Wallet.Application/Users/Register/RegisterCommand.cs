using MediatR;

namespace Wallet.Application.Users.Register
{
    public record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<Guid>
    {
        public sealed override string ToString() =>
            $"{nameof(RegisterCommand)} {{ DisplayName = {DisplayName}, Email = ***, Password = *** }}";
    }
}
