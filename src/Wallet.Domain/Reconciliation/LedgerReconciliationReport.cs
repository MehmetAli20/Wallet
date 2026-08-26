namespace Wallet.Domain.Reconciliation
{
    public sealed record LedgerReconciliationReport(
            IReadOnlyList<CurrencyLedgerTotals> LedgerTotals,
            IReadOnlyList<CurrencyNetPosition> NetPositions,
            IReadOnlyList<AccountDiscrepancy> AccountDiscrepancies,
            IReadOnlyList<ContextBalance> ContextBalances
            )
        {
        public bool IsBalanced =>
                LedgerTotals.All(t => t.IsBalanced)
                && NetPositions.All(p => p.IsBalanced)
                && AccountDiscrepancies.Count == 0
                && ContextBalances.All(c => c.IsBalanced);
        }
}