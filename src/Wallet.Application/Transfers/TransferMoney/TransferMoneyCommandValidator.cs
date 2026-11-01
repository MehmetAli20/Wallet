using FluentValidation;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Transfers.TransferMoney
{
    public class TransferMoneyCommandValidator : AbstractValidator<TransferMoneyCommand>
    {
        public TransferMoneyCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.RecipientUserId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(IIdempotentRequest.KeyMaxLength);
        }
    }
}