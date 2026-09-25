using MediatR;

namespace Wallet.Application.Groups.AddPlaceholder
{
    public record AddPlaceholderCommand(Guid GroupId, string DisplayName) : IRequest<Guid>;
}
