using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;

namespace Wallet.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly WalletDbContext _context;

        public UnitOfWork(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyConflictException("A concurrent modification was detected.", ex);
            }
            catch(DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
            {
                throw new UniqueConstraintViolationException("A record with the same unique value already exists", ex);
            }
        }
    }
}