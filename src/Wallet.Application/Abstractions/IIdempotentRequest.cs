using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions
{
    public interface IIdempotentRequest
    {
        const int KeyMaxLength = 100;

        string IdempotencyKey { get; }
    }
}
