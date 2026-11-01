using FluentValidation;
using Wallet.Application.Abstractions;
using Wallet.Domain.Common;

namespace Wallet.Application.Transfers.TransferMoney
{
    public class TransferMoneyCommandValidator : AbstractValidator<TransferMoneyCommand>
    {
        public TransferMoneyCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.RecipientUserId).NotEmpty();
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .LessThanOrEqualTo(Money.MaxAmount)
                .Must(Money.IsWholeCents)
                .WithMessage("Amount must be in whole cents.");
            RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(IIdempotentRequest.KeyMaxLength);
        }
    }
}