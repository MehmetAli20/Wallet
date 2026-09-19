using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Groups;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class GroupInviteLinkConfiguration : IEntityTypeConfiguration<GroupInviteLink>
    {
        public void Configure(EntityTypeBuilder<GroupInviteLink> builder)
        {
            builder.ToTable("GroupInviteLinks");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.TokenHash).IsRequired().HasMaxLength(64);
            builder.HasIndex(l => l.TokenHash).IsUnique();
            builder.HasIndex(l => l.GroupId);

            builder.Ignore(l => l.UseCount);

            builder.HasOne<Group>()
                .WithMany()
                .HasForeignKey(l => l.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Redemptions)
                .WithOne()
                .HasForeignKey(r => r.LinkId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata
                .FindNavigation(nameof(GroupInviteLink.Redemptions))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
