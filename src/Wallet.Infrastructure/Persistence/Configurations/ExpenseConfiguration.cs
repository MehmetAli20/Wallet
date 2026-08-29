using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Expenses;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("Expenses");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Description).HasMaxLength(200).IsRequired();
            builder.Property(e => e.OccurredAt);
            builder.Property(e => e.ReversalReason).HasMaxLength(200);

            builder.ComplexProperty(e => e.Total, m =>
            {
                m.Property(x => x.Amount).HasColumnName("Total").HasColumnType("numeric(19,4)");
                m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            builder.HasMany(e => e.Splits)
                .WithOne()
                .HasForeignKey(s => s.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata
                .FindNavigation(nameof(Expense.Splits))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasOne<Expense>()
                .WithMany()
                .HasForeignKey(e => e.ReplacesExpenseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => new { e.GroupId, e.OccurredAt });
        }
    }
}
