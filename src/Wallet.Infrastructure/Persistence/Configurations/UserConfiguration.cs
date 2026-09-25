using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DisplayName)
                .IsRequired()
                .HasMaxLength(User.DisplayNameMaxLength);
            builder.Property(x => x.Username)
                .HasMaxLength(50);
            builder.HasIndex(x => x.Username)
                .IsUnique();
            builder.Property(x => x.PasswordHash)
                .HasMaxLength(100);
            builder.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20);
            builder.Property(e => e.Email)
                .HasMaxLength(254);
            builder.HasIndex(e => e.Email)
                .IsUnique();
        }
    }
}
