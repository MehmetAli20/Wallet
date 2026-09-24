namespace Wallet.Api.Contracts.Groups.Responses
{
    public record GroupMemberResponse(Guid UserId, string Role, string Status);

    public record GroupResponse(
        Guid Id,
        string Kind,
        string? Name,
        string Currency,
        IReadOnlyList<GroupMemberResponse> Members);

    public record IssuedTokenResponse(
        string Kind,
        Guid Id,
        string Token,
        DateTimeOffset ExpiresAt,
        int MaxUses)
    {
        public sealed override string ToString() =>
            $"{nameof(IssuedTokenResponse)} {{ Kind = {Kind}, Id = {Id}, ExpiresAt = {ExpiresAt}, MaxUses = {MaxUses}, Token = *** }}";
    }

    public record GroupPlaceholderResponse(Guid UserId, string DisplayName);
}