using FluentValidation;

namespace Wallet.Application.Groups.ClaimPlaceholder
{
    public class ClaimPlaceholderCommandValidator : AbstractValidator<ClaimPlaceholderCommand>
    {
        public ClaimPlaceholderCommandValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(72);
        }
    }
}
