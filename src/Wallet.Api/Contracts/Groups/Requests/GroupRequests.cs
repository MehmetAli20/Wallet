namespace Wallet.Api.Contracts.Groups.Requests
{
    public record CreateGroupRequest(string Name, string Currency);
    public record InviteToGroupRequest(Guid UserId);
}