using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions.Accounts
{
    public interface IIdempotencyStore
    {
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        void Stage(string key, string requestName);
    }
}
