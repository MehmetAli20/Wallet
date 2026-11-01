using FluentValidation;
using Wallet.Application.Common;

namespace Wallet.Application.Groups.EnsurePair
{
    public class EnsurePairCommandValidator : AbstractValidator<EnsurePairCommand>
    {
        public EnsurePairCommandValidator()
        {
            RuleFor(x => x.OtherUserId).NotEmpty();
            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .Must(SupportedCurrencies.Contains)
                .WithMessage("Currency must be a supported ISO 4217 code.");
        }
    }
}
