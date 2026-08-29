namespace Wallet.Api.Contracts.Groups.Requests
{
    public record CreateGroupRequest(string Name, string Currency);
    public record InviteToGroupRequest(Guid UserId);
    public record EnsurePairRequest(Guid UserId, string Currency);
}