using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Infrastructure.Persistence.Idempotency;

namespace Wallet.Infrastructure.Persistence.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecords");
            builder.HasKey(x => new { x.UserId, x.Key });
            builder.Property(x => x.Key).HasMaxLength(IIdempotentRequest.KeyMaxLength);
            builder.Property(x => x.RequestName).HasMaxLength(200);
            builder.Property(x => x.RequestHash).HasMaxLength(64);
        }
    }
}
