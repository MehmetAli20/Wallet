using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Expenses;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpense>
    {
        public void Configure(EntityTypeBuilder<RecurringExpense> builder)
        {
            builder.ToTable("RecurringExpenses");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Description).HasMaxLength(200).IsRequired();
            builder.Property(r => r.Interval).HasConversion<int>();

            builder.ComplexProperty(r => r.Amount, m =>
            {
                m.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("numeric(19,4)");
                m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            builder.HasMany(r => r.Participants)
                .WithOne()
                .HasForeignKey(p => p.RecurringExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata
                .FindNavigation(nameof(RecurringExpense.Participants))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(r => new { r.IsActive, r.NextOccurrence });
        }
    }

    public class RecurringExpenseParticipantConfiguration
        : IEntityTypeConfiguration<RecurringExpenseParticipant>
    {
        public void Configure(EntityTypeBuilder<RecurringExpenseParticipant> builder)
        {
            builder.ToTable("RecurringExpenseParticipants");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.FixedShare).HasColumnType("numeric(19,4)");
            builder.HasIndex(p => new { p.RecurringExpenseId, p.UserId }).IsUnique();
        }
    }
}
