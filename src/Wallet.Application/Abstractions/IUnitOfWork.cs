using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}