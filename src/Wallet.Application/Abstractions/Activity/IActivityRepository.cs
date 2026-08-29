using Wallet.Domain.Activity;

namespace Wallet.Application.Abstractions.Activity
{
    public interface IActivityRepository
    {
        Task<IReadOnlyList<ActivityEntry>> GetForGroupAsync(
            Guid groupId, long? after, int limit, CancellationToken cancellationToken = default);
    }
}
