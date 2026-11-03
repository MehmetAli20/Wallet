using Wallet.Application.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface IUserBlockRepository
    {
        Task<bool> HasBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default);
        Task<UserBlock?> GetAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BlockedUser>> GetBlockedByAsync(Guid blockerId, CancellationToken cancellationToken = default);
        Task AddAsync(UserBlock block, CancellationToken cancellationToken = default);
        void Remove(UserBlock block);
    }
}
