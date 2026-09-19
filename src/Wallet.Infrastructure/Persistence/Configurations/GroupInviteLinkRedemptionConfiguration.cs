using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Groups;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class GroupInviteLinkRedemptionConfiguration : IEntityTypeConfiguration<GroupInviteLinkRedemption>
    {
        public void Configure(EntityTypeBuilder<GroupInviteLinkRedemption> builder)
        {
            builder.ToTable("GroupInviteLinkRedemptions");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedNever();

            builder.HasIndex(r => new { r.LinkId, r.UserId }).IsUnique();
        }
    }
}
