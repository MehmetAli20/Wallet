using Wallet.Domain.Reconciliation;

namespace Wallet.Application.Abstractions
{
    public interface IReconciliationRepository
    {
        Task<IReadOnlyList<CurrencyBalance>> GetCurrencyBalancesAsync(CancellationToken cancellationToken = default);
    }
}