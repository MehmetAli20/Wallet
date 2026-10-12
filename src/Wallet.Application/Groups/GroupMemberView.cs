using Wallet.Domain.Groups;

namespace Wallet.Application.Groups
{
    public sealed record GroupMemberView(
        Guid UserId,
        string DisplayName,
        bool IsPlaceholder,
        GroupMemberRole Role,
        GroupMemberStatus Status);
}
