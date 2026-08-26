namespace Wallet.Domain.Reconciliation
{
    public sealed record ContextBalance(Guid GroupId, decimal Net)
    {
        public bool IsBalanced => Net == 0m;
    }
}
