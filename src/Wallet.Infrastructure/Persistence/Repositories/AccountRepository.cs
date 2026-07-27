using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Domain.Accounts;

namespace Wallet.Infrastructure.Persistence.Repositories
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
                .Include(a => a.Entries.OrderBy(e => e.Sequence))
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            await _context.Accounts.AddAsync(account, cancellationToken);
            //AddAsync'in async olmasının tek sebebi özel değer üreteçleridir
            //(ör. SQL Server'ın HiLo stratejisi — bir sonraki id bloğunu almak için DB'ye gitmesi gerekir).
            //Bizde Guid id'yi ben üretiyorum (Guid.NewGuid()),
            //yani DB'ye gitmeye gerek yok — senkron Add de tamamen yeterliydi.
        }
    }
}