namespace Wallet.Api.Contracts.Groups.Responses
{
    public record GroupMemberResponse(Guid UserId, string Role, string Status);

    public record GroupResponse(
        Guid Id,
        string Kind,
        string? Name,
        string Currency,
        IReadOnlyList<GroupMemberResponse> Members);
}