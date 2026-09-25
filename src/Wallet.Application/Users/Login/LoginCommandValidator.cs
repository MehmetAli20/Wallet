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
            RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
            RuleFor(x => x.Password).NotEmpty();
            RuleFor(x => x.Purpose).IsInEnum();
        }
    }
}
