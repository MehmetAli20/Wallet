using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;

namespace Wallet.Application.Abstractions.Accounts
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(Account account, CancellationToken cancellationToken = default);
    }
}