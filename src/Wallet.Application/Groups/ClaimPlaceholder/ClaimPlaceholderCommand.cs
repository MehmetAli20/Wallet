using MediatR;

namespace Wallet.Application.Groups.ClaimPlaceholder
{
    public record ClaimPlaceholderCommand(
        string Token,
        string Email,
        string Password) : IRequest<Guid>
    {
        public sealed override string ToString() =>
            $"{nameof(ClaimPlaceholderCommand)} {{ Token = ***, Email = ***, Password = *** }}";
    }
}
