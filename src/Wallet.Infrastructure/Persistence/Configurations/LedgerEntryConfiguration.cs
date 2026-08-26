using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Accounts;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
    {
        public void Configure(EntityTypeBuilder<LedgerEntry> builder)
        {
            builder.ToTable("LedgerEntries");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);

            builder.ComplexProperty(e => e.Amount, m =>
            {
                m.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("numeric(19,4)");
                m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            builder.Property(e => e.OccurredAt);
            builder.Property(e => e.Sequence);

            builder.HasIndex(e => new { e.AccountId, e.Sequence });
            builder.Property(e => e.GroupId);

            builder.HasIndex(e => e.OwnerId);
            builder.HasIndex(e => new { e.GroupId, e.OwnerId });
        }
    }
}