using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Domain.Accounts;

namespace Wallet.Infrastructure.Persistence.Repositories.Accounts
{
    public class AccountRepository : IAccountRepository
    {
        private readonly WalletDbContext _context;

        public AccountRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<LedgerEntry>> GetEntriesAsync(
            Guid accountId, int skip, int take, CancellationToken cancellationToken = default)
        {
            return await _context.Set<LedgerEntry>()
                .Where(e => e.AccountId == accountId)
                .OrderByDescending(e => e.Sequence)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            await _context.Accounts.AddAsync(account, cancellationToken);
            //AddAsync'in async olmasının tek sebebi özel değer üreteçleridir
            //(ör. SQL Server'ın HiLo stratejisi — bir sonraki id bloğunu almak için DB'ye gitmesi gerekir).
            //Bizde Guid id'yi ben üretiyorum (Guid.NewGuid()),
            //yani DB'ye gitmeye gerek yok — senkron Add de tamamen yeterliydi.
        }

        public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Accounts
                .OrderBy(a => a.Currency)
                .ToListAsync();
        }

        public async Task<Account?> GetByOwnerAndCurrencyAsync(Guid ownerId, string currency, CancellationToken cancellationToken = default)
        {
            return await _context.Accounts
                .IgnoreQueryFilters()
                .Include(a=>a.Entries)
                .FirstOrDefaultAsync(a=>a.OwnerId == ownerId && a.Currency == currency, cancellationToken);
        }
    }
}