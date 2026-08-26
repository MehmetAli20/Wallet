using Wallet.Domain.Groups;

namespace Wallet.Application.Abstractions.Groups
{
    public interface IGroupBalanceRepository
    {
        Task<IReadOnlyList<MemberPosition>> GetPositionsAsync(Guid groupId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PairwiseDebt>> GetDebtsAsync(Guid groupId, CancellationToken cancellationToken = default);
    }
}
