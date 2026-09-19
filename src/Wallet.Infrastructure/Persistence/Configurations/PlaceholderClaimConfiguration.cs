using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class PlaceholderClaimConfiguration : IEntityTypeConfiguration<PlaceholderClaim>
    {
        public void Configure(EntityTypeBuilder<PlaceholderClaim> builder)
        {
            builder.ToTable("PlaceholderClaims");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.TokenHash).IsRequired().HasMaxLength(64);
            builder.HasIndex(c => c.TokenHash).IsUnique();
            builder.HasIndex(c => c.PlaceholderUserId);
        }
    }
}
