using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Accounts;

namespace Wallet.Infrastructure.Persistence
{
    public class WalletDbContext : DbContext
    {
        public WalletDbContext(DbContextOptions<WalletDbContext> options) 
            : base(options)
        {

        }

        public DbSet<Account> Accounts => Set<Account>();

        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            modelbuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
            base.OnModelCreating(modelbuilder);
        }
    }
}