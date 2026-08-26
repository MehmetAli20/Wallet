using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Expenses;
using Wallet.Domain.Groups;
using Wallet.Domain.Transfers;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence
{
    public class WalletDbContext : DbContext
    {
        private readonly ICurrentUser _currentUser;
        public WalletDbContext(DbContextOptions<WalletDbContext> options, ICurrentUser currentUser) 
            : base(options)
        {
            _currentUser = currentUser;
        }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<User> Users => Set<User>();
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<ScheduledTransfer> ScheduledTransfers => Set<ScheduledTransfer>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<Expense> Expenses => Set<Expense>();
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            modelbuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
            modelbuilder.Entity<Account>()
                .HasQueryFilter(a => _currentUser.IsSystem || a.OwnerId == _currentUser.UserId);
            modelbuilder.Entity<LedgerEntry>()
                .HasQueryFilter(e => _currentUser.IsSystem
                    || e.OwnerId == _currentUser.UserId
                    || Set<Group>().Any(g => g.Id == e.GroupId
                        && g.Members.Any(m => m.UserId == _currentUser.UserId && m.Status == GroupMemberStatus.Active)));
            modelbuilder.Entity<Group>()
                .HasQueryFilter(g => _currentUser.IsSystem
                    || g.Members.Any(m => m.UserId == _currentUser.UserId && m.Status == GroupMemberStatus.Active));
            base.OnModelCreating(modelbuilder);
        }
    }
}