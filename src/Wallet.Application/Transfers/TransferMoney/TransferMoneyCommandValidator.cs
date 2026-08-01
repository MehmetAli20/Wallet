using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Transfers.TransferMoney
{
    public class TransferMoneyCommandValidator : AbstractValidator<TransferMoneyCommand>
    {
        public TransferMoneyCommandValidator()
        {
            RuleFor(x => x.SourceId).NotEmpty();
            RuleFor(x => x.DestinationId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Za-z]+$");
            RuleFor(x => x.IdempotencyKey).NotEmpty();
        }
    }
}