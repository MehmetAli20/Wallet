using MediatR;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.IssueInviteLinks
{
    public record IssueInviteLinkCommand(Guid GroupId, Guid? PlaceholderUserId, int? MaxUses)
        : IRequest<IssuedGroupToken>;
}
