using MediatR;

namespace Wallet.Application.Groups.ClaimPlaceholder
{
    public record ClaimPlaceholderCommand(
        string Token,
        string Username,
        string Email,
        string Password) : IRequest<Guid>
    {
        public sealed override string ToString() =>
            $"{nameof(ClaimPlaceholderCommand)} {{ Username = {Username}, Token = ***, Email = ***, Password = *** }}";
    }
}
