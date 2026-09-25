using FluentValidation;

namespace Wallet.Application.Groups.AddPlaceholder
{
    public class AddPlaceholderCommandValidator : AbstractValidator<AddPlaceholderCommand>
    {
        public AddPlaceholderCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        }
    }
}
