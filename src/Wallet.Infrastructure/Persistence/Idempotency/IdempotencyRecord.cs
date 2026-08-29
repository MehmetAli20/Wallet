using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Infrastructure.Persistence.Idempotency
{
    public class IdempotencyRecord
    {
        public Guid UserId { get; private set; }
        public string Key { get; private set; } = null!;
        public string RequestName { get; private set; } = null!;
        public DateTimeOffset CreatedAt { get; private set; }
        public string? Response { get; private set; }

        private IdempotencyRecord() { }

        public IdempotencyRecord(Guid userId, string key, string requestName, DateTimeOffset createdAt)
        {
            UserId = userId;
            Key = key;
            RequestName = requestName;
            CreatedAt = createdAt;
        }

        public void SetResponse(string response) => Response = response;
    }
}
