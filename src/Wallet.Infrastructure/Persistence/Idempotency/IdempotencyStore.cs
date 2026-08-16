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

    public async Task<IdempotencyLookup> FindAsync(string key, CancellationToken cancellationToken = default)
    {
        var record = await _context.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        return record is null
            ? new IdempotencyLookup(false, null)
            : new IdempotencyLookup(true, record.Response);
    }

    public void Stage(string key, string requestName) =>
        _context.Add(new IdempotencyRecord(key, requestName, DateTimeOffset.UtcNow));

    public async Task SetResponseAsync(string key, string response, CancellationToken cancellationToken = default)
    {
        var record = await _context.Set<IdempotencyRecord>().FindAsync([key], cancellationToken)
            ?? throw new InvalidOperationException($"Idempotency record '{key}' was not found.");

        record.SetResponse(response);
    }
}