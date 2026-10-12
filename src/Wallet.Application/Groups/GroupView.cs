using Wallet.Domain.Groups;

namespace Wallet.Application.Groups
{
    public sealed record GroupView(
        Guid Id,
        GroupKind Kind,
        string? Name,
        string Currency,
        IReadOnlyList<GroupMemberView> Members);
}
