using MediatR;

namespace Wallet.Application.Groups.IssueClaimToken
{
    public record IssueClaimTokenCommand(Guid PlaceholderUserId) : IRequest<string>;
}
