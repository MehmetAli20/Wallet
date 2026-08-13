using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions.Exceptions
{
    public class UniqueConstraintViolationException : Exception
    {
        public UniqueConstraintViolationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}