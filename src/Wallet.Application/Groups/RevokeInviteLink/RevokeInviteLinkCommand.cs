using MediatR;

namespace Wallet.Application.Groups.RevokeInviteLink
{
    public record RevokeInviteLinkCommand(Guid GroupId, Guid LinkId) : IRequest;
}
