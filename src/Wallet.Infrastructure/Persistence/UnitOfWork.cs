using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;

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
        }
    }
}
