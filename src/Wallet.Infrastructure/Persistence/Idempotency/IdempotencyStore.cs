using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Infrastructure.Persistence.Idempotency;

public class IdempotencyStore : IIdempotencyStore
{
    private readonly WalletDbContext _context;
    private readonly ICurrentUser _currentUser;

    public IdempotencyStore(WalletDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IdempotencyLookup> FindAsync(
        string key, string requestName, string requestHash, CancellationToken cancellationToken = default)
    {
        var record = await _context.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r => r.UserId == _currentUser.UserId && r.Key == key, cancellationToken);

        if (record is null)
            return new IdempotencyLookup(false, null);

        if (record.RequestName != requestName)
            throw new IdempotencyKeyReuseException(key, record.RequestName, requestName);

        if (record.RequestHash is not null && record.RequestHash != requestHash)
            throw new IdempotencyPayloadMismatchException(key);

        return new IdempotencyLookup(true, record.Response);
    }

    public void Stage(string key, string requestName, string? requestHash) =>
        _context.Add(new IdempotencyRecord(
            _currentUser.UserId, key, requestName, requestHash, DateTimeOffset.UtcNow));

    public async Task SetResponseAsync(string key, string response, CancellationToken cancellationToken = default)
    {
        var record = await _context.Set<IdempotencyRecord>()
            .FindAsync([_currentUser.UserId, key], cancellationToken)
            ?? throw new InvalidOperationException($"Idempotency record '{key}' was not found.");

        record.SetResponse(response);
    }
}
