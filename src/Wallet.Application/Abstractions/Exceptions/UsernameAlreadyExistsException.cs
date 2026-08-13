using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Abstractions.Exceptions
{
    public class UsernameAlreadyExistsException : Exception
    {
        public UsernameAlreadyExistsException() : base("This username is already taken.")
        {
        }
    }
}
