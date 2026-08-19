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
            builder.Property<uint>("xmin").IsRowVersion();

            builder.Ignore(a => a.Balance);
            builder.Property<decimal>("_balanceAmount")
                .HasColumnName("Balance")
                .HasColumnType("numeric(19,4)");

            builder.Property(a => a.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();

            builder.HasIndex(a => new { a.OwnerId, a.Currency }).IsUnique();

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