using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
    {
        public void Configure(EntityTypeBuilder<UserReport> builder)
        {
            builder.ToTable("UserReports");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Reason)
                .HasConversion<string>()
                .HasMaxLength(30);
            builder.Property(x => x.Details)
                .HasMaxLength(UserReport.DetailsMaxLength);
            builder.HasIndex(x => new { x.ReporterId, x.ReportedUserId })
                .IsUnique()
                .HasFilter("\"ResolvedAt\" IS NULL");
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.ReportedUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
