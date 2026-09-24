namespace Wallet.Api.Contracts.Groups.Requests
{
    public record CreateGroupRequest(string Name, string Currency);
    public record InviteToGroupRequest(Guid UserId);
    public record EnsurePairRequest(Guid UserId, string Currency);
    public record IssueInviteLinkRequest(Guid? PlaceholderUserId, int? MaxUses);
    public record JoinGroupRequest(string Token)
    {
        public sealed override string ToString() =>
            $"{nameof(JoinGroupRequest)} {{ Token = *** }}";
    }
}