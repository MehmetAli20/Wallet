using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Accounts.CreateAccount
{
    public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
    {
        public CreateAccountCommandValidator()
        {
            RuleFor(x=>x.Amount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Za-z]+$");
        }
    }
}