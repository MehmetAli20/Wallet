using FluentValidation;

namespace Wallet.Application.Transfers.TransferMoney
{
    public class TransferMoneyCommandValidator : AbstractValidator<TransferMoneyCommand>
    {
        public TransferMoneyCommandValidator()
        {
            RuleFor(x => x.RecipientUserId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Za-z]+$");
            RuleFor(x => x.IdempotencyKey).NotEmpty();
        }
    }
}