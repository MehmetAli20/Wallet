using Wallet.Domain.Reconciliation;

namespace Wallet.Application.Abstractions
{
    public interface IReconciliationRepository
    {
        Task<IReadOnlyList<CurrencyLedgerTotals>> GetLedgerTotalsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CurrencyNetPosition>> GetNetPositionsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AccountDiscrepancy>> GetAccountDiscrepanciesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ContextBalance>> GetContextBalancesAsync(CancellationToken cancellationToken = default);
    }
}