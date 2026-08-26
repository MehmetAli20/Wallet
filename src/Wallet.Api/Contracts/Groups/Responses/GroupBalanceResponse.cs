namespace Wallet.Api.Contracts.Groups.Responses
{
    public record MemberPositionResponse(Guid UserId, decimal Net);

    public record PairwiseDebtResponse(Guid DebtorId, Guid CreditorId, decimal Amount);

    public record GroupBalanceResponse(
        Guid GroupId,
        string Currency,
        IReadOnlyList<MemberPositionResponse> Positions,
        IReadOnlyList<PairwiseDebtResponse> Debts);
}
