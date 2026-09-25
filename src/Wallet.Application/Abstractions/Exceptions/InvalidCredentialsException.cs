using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("Invalid email or password.")
        {
        }
    }
}
