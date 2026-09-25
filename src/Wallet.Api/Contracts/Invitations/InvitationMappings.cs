using Wallet.Api.Contracts.Invitations.Responses;
using Wallet.Domain.Groups;

namespace Wallet.Api.Contracts.Invitations
{
    public static class InvitationMappings
    {
        public static InvitationResponse ToResponse(this PendingInvitation invitation) =>
            new(invitation.InvitationId,
                invitation.GroupId,
                invitation.GroupName,
                invitation.Currency,
                invitation.InvitedAt,
                invitation.InvitedByUserId,
                invitation.InvitedByDisplayName);
    }
}
