using Wallet.Domain.Groups;

namespace Wallet.Application.Abstractions.Groups
{
    public interface IGroupRepository
    {
        Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Group?> GetPairAsync(Guid userA, Guid userB, string currency, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Group group, CancellationToken cancellationToken = default);
    }
}
