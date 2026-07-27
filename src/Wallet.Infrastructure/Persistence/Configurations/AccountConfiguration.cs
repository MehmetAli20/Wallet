using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");
            builder.HasKey(a => a.Id);

            builder.ComplexProperty(a => a.Balance, b =>
            {
                b.Property(m => m.Amount).HasColumnName("Balance").HasColumnType("numeric(19,4)");
                b.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            builder.HasMany(a => a.Entries)
                .WithOne()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Metadata
                .FindNavigation(nameof(Account.Entries))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}