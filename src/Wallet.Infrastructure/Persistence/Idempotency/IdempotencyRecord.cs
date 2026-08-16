using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Infrastructure.Persistence.Idempotency
{
    public class IdempotencyRecord
    {
        public string Key { get; private set; } = null!;
        public string RequestName { get; private set; } = null!;
        public DateTimeOffset CreatedAt { get; private set; }
        public string? Response { get; private set; }

        private IdempotencyRecord() { }

        public IdempotencyRecord(string key, string requestName, DateTimeOffset createdAt)
        {
            Key = key;
            RequestName = requestName;
            CreatedAt = createdAt;
        }

        public void SetResponse(string response) => Response = response;
    }
}
