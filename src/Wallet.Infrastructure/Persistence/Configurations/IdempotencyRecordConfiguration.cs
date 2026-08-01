using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Infrastructure.Persistence.Idempotency;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecords");
            builder.HasKey(x => x.Key);
            builder.Property(x => x.Key).HasMaxLength(100);
            builder.Property(x => x.RequestName).HasMaxLength(200);
        }
    }
}
