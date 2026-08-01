using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;

namespace Wallet.Infrastructure.Persistence.Idempotency;

public class IdempotencyStore : IIdempotencyStore
{
    private readonly WalletDbContext _context;

    public IdempotencyStore(WalletDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        _context.Set<IdempotencyRecord>().AnyAsync(r => r.Key == key, cancellationToken);

    public void Stage(string key, string requestName) =>
        _context.Add(new IdempotencyRecord(key, requestName, DateTimeOffset.UtcNow));
}