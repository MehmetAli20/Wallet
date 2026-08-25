namespace Wallet.Domain.Groups
{
    public sealed record PendingInvitation(
        Guid InvitationId,
        Guid GroupId,
        string? GroupName,
        string Currency,
        DateTimeOffset InvitedAt,
        Guid InvitedByUserId,
        string InvitedByUsername);
}
