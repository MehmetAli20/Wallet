using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Accounts;
using Wallet.Domain.Transfers;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence
{
    public class WalletDbContext : DbContext
    {
        public WalletDbContext(DbContextOptions<WalletDbContext> options) 
            : base(options)
        {

        }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<ScheduledTransfer> ScheduledTransfers => Set<ScheduledTransfer>();
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            modelbuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
            base.OnModelCreating(modelbuilder);
        }
    }
}