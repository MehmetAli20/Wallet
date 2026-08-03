namespace Wallet.Domain.Reconciliation
{
    public sealed record LedgerReconciliationReport(IReadOnlyList<CurrencyBalance> Balances)
    {
        public bool IsBalanced => Balances.All(b => b.IsBalanced);

        public static LedgerReconciliationReport From(IEnumerable<CurrencyBalance> balances) =>
            new(balances.ToList());
    }
}