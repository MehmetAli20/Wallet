using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Groups;

namespace Wallet.Application.Abstractions.Groups
{
    public interface IGroupInviteLinkRepository
    {
        Task AddAsync(GroupInviteLink groupInviteLink, CancellationToken cancellationToken = default);
        Task <GroupInviteLink?> GetUsableAsync(string token, DateTimeOffset asOf, CancellationToken cancellationToken = default);
        Task <GroupInviteLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GroupInviteLink>> GetOutstandingForGroupAsync(Guid groupId, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    }
}
