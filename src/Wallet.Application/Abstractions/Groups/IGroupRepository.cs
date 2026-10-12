using Wallet.Application.Groups;
using Wallet.Domain.Groups;

namespace Wallet.Application.Abstractions.Groups
{
    public interface IGroupRepository
    {
        Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Group?> GetPairAsync(Guid userA, Guid userB, string currency, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GroupView>> GetMyGroupViewsAsync(CancellationToken cancellationToken = default);
        Task<GroupView?> GetGroupViewAsync(Guid groupId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Group>> GetAllForPartyAsync(Guid partyId, CancellationToken cancellationToken = default);
        Task AddAsync(Group group, CancellationToken cancellationToken = default);
        Task<Group?> GetByInvitationIdAsync(Guid invitationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PendingInvitation>> GetPendingInvitationsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Group?> GetByInviteTokenAsync(string token, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    }
}