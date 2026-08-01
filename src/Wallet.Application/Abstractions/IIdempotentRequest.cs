using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions
{
    public interface IIdempotentRequest
    {
        string IdempotencyKey { get; }
    }
}
