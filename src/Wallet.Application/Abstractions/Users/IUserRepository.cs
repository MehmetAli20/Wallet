using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface IUserRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetPlaceholderInMyGroupsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> DisplayNameTakenInGroupAsync(Guid groupId, string displayName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<User>> GetPlaceholdersInGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    }
}