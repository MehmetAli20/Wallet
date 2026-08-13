using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions.Exceptions
{
    public class EmailAlreadyExistsException : Exception
    {
        public EmailAlreadyExistsException()
            : base("This email is already registered.")
        {
        }
    }
}
