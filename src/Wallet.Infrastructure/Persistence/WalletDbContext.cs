using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Activity;
using Wallet.Domain.Expenses;
using Wallet.Domain.Groups;
using Wallet.Domain.Settlements;
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
        public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();
        public DbSet<PlaceholderClaim> PlaceholderClaims => Set<PlaceholderClaim>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<GroupInviteLink> GroupInviteLinks => Set<GroupInviteLink>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Settlement> Settlements => Set<Settlement>();
        public DbSet<ActivityEntry> ActivityEntries => Set<ActivityEntry>();
        public DbSet<GroupActivityRead> GroupActivityReads => Set<GroupActivityRead>();
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            modelbuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
            modelbuilder.Entity<Account>()
                .HasQueryFilter(a => _currentUser.IsSystem || a.OwnerId == _currentUser.UserId);
            modelbuilder.Entity<Expense>()
                .HasQueryFilter(e => _currentUser.IsSystem 
                    || Set<Group>().Any(g => g.Id == e.GroupId 
                        && g.Members.Any(m => m.UserId == _currentUser.UserId 
                            && m.Status == GroupMemberStatus.Active)));
            modelbuilder.Entity<LedgerEntry>()
                .HasQueryFilter(e => _currentUser.IsSystem
                    || e.OwnerId == _currentUser.UserId
                    || Set<Group>().Any(g => g.Id == e.GroupId
                        && g.Members.Any(m => m.UserId == _currentUser.UserId && m.Status == GroupMemberStatus.Active)));
            modelbuilder.Entity<Settlement>()
                .HasQueryFilter(s => _currentUser.IsSystem
                    || Set<Group>().Any(g => g.Id == s.GroupId
                        && g.Members.Any(m => m.UserId == _currentUser.UserId
                            && m.Status == GroupMemberStatus.Active)));
            modelbuilder.Entity<ActivityEntry>()
                .HasQueryFilter(a => _currentUser.IsSystem
                    || Set<Group>().Any(g => g.Id == a.GroupId
                        && g.Members.Any(m => m.UserId == _currentUser.UserId
                            && m.Status == GroupMemberStatus.Active)));
            modelbuilder.Entity<RecurringExpense>()
                .HasQueryFilter(r => _currentUser.IsSystem
                    || Set<Group>().Any(g => g.Id == r.GroupId
                        && g.Members.Any(m => m.UserId == _currentUser.UserId
                            && m.Status == GroupMemberStatus.Active)));
            modelbuilder.Entity<GroupActivityRead>()
                .HasQueryFilter(r => _currentUser.IsSystem || r.UserId == _currentUser.UserId);
            modelbuilder.Entity<Group>()
                .HasQueryFilter(g => _currentUser.IsSystem
                    || g.Members.Any(m => m.UserId == _currentUser.UserId && m.Status == GroupMemberStatus.Active));
            base.OnModelCreating(modelbuilder);
        }
    }
}