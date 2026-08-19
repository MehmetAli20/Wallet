namespace Wallet.Domain.Reconciliation
{
    public sealed record LedgerReconciliationReport(
            IReadOnlyList<CurrencyLedgerTotals> LedgerTotals,
            IReadOnlyList<CurrencyNetPosition> NetPositions,
            IReadOnlyList<AccountDiscrepancy> AccountDiscrepancies
            )
        {
        public bool IsBalanced =>
                LedgerTotals.All(t => t.IsBalanced)
                && NetPositions.All(p => p.IsBalanced)
                && AccountDiscrepancies.Count == 0;
        }
}