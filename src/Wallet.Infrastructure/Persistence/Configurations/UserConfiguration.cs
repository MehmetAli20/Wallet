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
            builder.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(50);
            builder.HasIndex(x => x.Username)
                .IsUnique();
            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}
