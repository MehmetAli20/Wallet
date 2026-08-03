namespace Wallet.Domain.Reconciliation
{
    public sealed record CurrencyBalance(string Currency, decimal TotalCredits, decimal TotalDebits)
    {
        public decimal Discrepancy => TotalCredits - TotalDebits;
        public bool IsBalanced => Discrepancy == 0m;
    }
}