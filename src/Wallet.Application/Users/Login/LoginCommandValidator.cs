using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Users.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}
