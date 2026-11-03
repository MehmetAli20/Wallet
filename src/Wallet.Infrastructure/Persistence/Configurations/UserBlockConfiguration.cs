using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
    {
        public void Configure(EntityTypeBuilder<UserBlock> builder)
        {
            builder.ToTable("UserBlocks");
            builder.HasKey(x => new { x.BlockerId, x.BlockedId });
            builder.HasIndex(x => x.BlockedId);
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.BlockerId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.BlockedId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
