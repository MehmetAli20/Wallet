using FluentValidation;

namespace Wallet.Application.Groups.ClaimPlaceholder
{
    public class ClaimPlaceholderCommandValidator : AbstractValidator<ClaimPlaceholderCommand>
    {
        public ClaimPlaceholderCommandValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50)
                .Matches("^[a-zA-Z0-9._-]+$");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(72);
        }
    }
}
