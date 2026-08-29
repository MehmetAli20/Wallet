using Wallet.Domain.Activity;

namespace Wallet.Application.Abstractions.Activity
{
    public interface IActivityRepository
    {
        Task<IReadOnlyList<ActivityEntry>> GetForGroupAsync(
            Guid groupId, long? after, int limit, CancellationToken cancellationToken = default);

        Task<GroupActivityRead?> GetReadCursorAsync(
            Guid groupId, CancellationToken cancellationToken = default);

        Task AddReadCursorAsync(
            GroupActivityRead cursor, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GroupUnreadCount>> GetUnreadCountsAsync(
            CancellationToken cancellationToken = default);
    }
}
