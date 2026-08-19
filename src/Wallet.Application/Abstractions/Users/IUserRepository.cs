using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}