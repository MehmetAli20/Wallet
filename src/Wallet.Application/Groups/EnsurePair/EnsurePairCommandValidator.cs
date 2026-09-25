using FluentValidation;

namespace Wallet.Application.Groups.EnsurePair
{
    public class EnsurePairCommandValidator : AbstractValidator<EnsurePairCommand>
    {
        public EnsurePairCommandValidator()
        {
            RuleFor(x => x.OtherUserId).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty().Length(3);
        }
    }
}
