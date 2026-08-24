using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Groups;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("Groups");
            builder.HasKey(g => g.Id);

            builder.Property(g => g.Kind).HasConversion<string>().HasMaxLength(20);
            builder.Property(g => g.Name).HasMaxLength(100);
            builder.Property(g => g.Currency).HasMaxLength(3).IsRequired();
            builder.Property(g => g.PairKey).HasMaxLength(65);

            builder.HasIndex(g => new { g.PairKey, g.Currency }).IsUnique();

            builder.HasMany(g => g.Members)
                .WithOne()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata
                .FindNavigation(nameof(Group.Members))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
