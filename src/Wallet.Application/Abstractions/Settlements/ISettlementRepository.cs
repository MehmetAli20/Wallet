using Wallet.Domain.Settlements;

namespace Wallet.Application.Abstractions.Settlements
{
    public interface ISettlementRepository
    {
        Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default);
        Task<Settlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
