namespace Wallet.Domain.Groups
{
    public sealed record MemberPosition(Guid UserId, decimal Net);

    public sealed record PairwiseDebt(Guid DebtorId, Guid CreditorId, decimal Amount);

    public sealed record GroupBalanceReport(
        Guid GroupId,
        string Currency,
        IReadOnlyList<MemberPosition> Positions,
        IReadOnlyList<PairwiseDebt> Debts)
    {
        public bool NetsToZero => Positions.Sum(p => p.Net) == 0m;
    }
}
