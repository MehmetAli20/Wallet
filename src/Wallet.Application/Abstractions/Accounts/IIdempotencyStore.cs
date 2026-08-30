using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions.Accounts
{
    public record IdempotencyLookup(bool Exists, string? Response);

    public interface IIdempotencyStore
    {
        Task<IdempotencyLookup> FindAsync(
            string key, string requestName, string requestHash, CancellationToken cancellationToken = default);
        void Stage(string key, string requestName, string? requestHash);
        Task SetResponseAsync(string key, string response, CancellationToken cancellationToken = default);
    }
}
