namespace Wallet.Api.Contracts.Invitations.Responses
{
    public record InvitationResponse(
        Guid Id,
        Guid GroupId,
        string? GroupName,
        string Currency,
        DateTimeOffset InvitedAt,
        Guid InvitedByUserId,
        string InvitedByDisplayName);
}
