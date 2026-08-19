namespace Wallet.Domain.Reconciliation
{
    public sealed record CurrencyLedgerTotals(string Currency, decimal TotalCredits, decimal TotalDebits)
    {
        public decimal Discrepancy => TotalCredits - TotalDebits;
        public bool IsBalanced => Discrepancy == 0m;
    }

    public sealed record CurrencyNetPosition(string Currency, decimal TotalBalance)
    {
        public bool IsBalanced => TotalBalance == 0m;
    }

    public sealed record AccountDiscrepancy(Guid AccountId, string Currency, decimal Balance, decimal LedgerNet)
    {
        public decimal Difference => Balance - LedgerNet;
    }
}