using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Transfers;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class ScheduledTransferConfiguration : IEntityTypeConfiguration<ScheduledTransfer>
    {
        public void Configure(EntityTypeBuilder<ScheduledTransfer> builder)
        {
            builder.ToTable("ScheduledTransfers");
            builder.HasKey(g => g.Id);
            builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.ComplexProperty(x => x.Amount, m =>
            {
                m.Property(a => a.Amount).HasColumnName("Amount").HasColumnType("numeric(19,4)");
                m.Property(a => a.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            builder.Property(x=>x.FailureReason).HasMaxLength(500);
            builder.HasIndex(x => new { x.Status, x.ScheduledFor });
        }
    }
}
