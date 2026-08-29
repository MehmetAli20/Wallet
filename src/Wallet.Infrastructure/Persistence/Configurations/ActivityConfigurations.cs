using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Activity;
using Wallet.Domain.Settlements;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class ActivityEntryConfiguration : IEntityTypeConfiguration<ActivityEntry>
    {
        public void Configure(EntityTypeBuilder<ActivityEntry> builder)
        {
            builder.ToTable("ActivityEntries");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Sequence)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn();

            builder.HasAlternateKey(a => a.Sequence);

            builder.Property(a => a.Type).HasConversion<int>();
            builder.Property(a => a.Currency).HasMaxLength(3);
            builder.Property(a => a.Description).HasMaxLength(200);
            builder.Property(a => a.Amount).HasColumnType("numeric(19,4)");

            builder.HasIndex(a => new { a.GroupId, a.Sequence });
        }
    }

    public class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
    {
        public void Configure(EntityTypeBuilder<Settlement> builder)
        {
            builder.ToTable("Settlements");
            builder.HasKey(s => s.Id);

            builder.ComplexProperty(s => s.Amount, m =>
            {
                m.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("numeric(19,4)");
                m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            builder.HasIndex(s => new { s.GroupId, s.OccurredAt });
        }
    }
}
