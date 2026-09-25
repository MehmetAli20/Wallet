using FluentValidation;
using Wallet.Domain.Users;

namespace Wallet.Application.Groups.AddPlaceholder
{
    public class AddPlaceholderCommandValidator : AbstractValidator<AddPlaceholderCommand>
    {
        public AddPlaceholderCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.DisplayName)
                .Must(name => User.TryNormalizeDisplayName(name, out _))
                .WithMessage($"Display name must be 1-{User.DisplayNameMaxLength} characters and cannot contain control or invisible characters.");
        }
    }
}
