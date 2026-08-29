using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Domain.Common;
using Wallet.Infrastructure.Persistence.Activity;

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
            RecordActivity();

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

        private void RecordActivity()
        {
            var roots = _context.ChangeTracker
                .Entries<AggregateRoot>()
                .Select(entry => entry.Entity)
                .Where(root => root.DomainEvents.Count > 0)
                .ToList();

            if (roots.Count == 0)
                return;

            foreach (var root in roots)
            {
                foreach (var domainEvent in root.DomainEvents)
                {
                    var entry = ActivityEntryFactory.From(domainEvent);

                    if (entry is not null)
                        _context.Add(entry);
                }

                root.ClearDomainEvents();
            }
        }
    }
}