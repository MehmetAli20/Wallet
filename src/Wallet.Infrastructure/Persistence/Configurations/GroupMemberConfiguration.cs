using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Groups;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
    {
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            builder.ToTable("GroupMembers");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).ValueGeneratedNever();

            builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
            builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

            builder.Property(m => m.InvitedAt);
            builder.Property(m => m.JoinedAt);

            builder.HasIndex(m => new { m.GroupId, m.UserId }).IsUnique();
            builder.HasIndex(m => m.UserId);
        }
    }
}
