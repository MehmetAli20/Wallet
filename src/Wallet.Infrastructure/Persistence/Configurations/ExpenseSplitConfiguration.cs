using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Expenses;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class ExpenseSplitConfiguration : IEntityTypeConfiguration<ExpenseSplit>
    {
        public void Configure(EntityTypeBuilder<ExpenseSplit> builder)
        {
            builder.ToTable("ExpenseSplits");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.ComplexProperty(s => s.Share, m =>
            {
                m.Property(x => x.Amount).HasColumnName("Share").HasColumnType("numeric(19,4)");
                m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            builder.HasIndex(s => new { s.ExpenseId, s.ParticipantId }).IsUnique();
        }
    }
}
