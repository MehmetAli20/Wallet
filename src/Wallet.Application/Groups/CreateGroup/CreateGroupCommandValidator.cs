using FluentValidation;
using Wallet.Application.Common;

namespace Wallet.Application.Groups.CreateGroup
{
    public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
    {
        public CreateGroupCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .Matches("^[A-Za-z]+$")
                .Must(SupportedCurrencies.Contains)
                .WithMessage("Currency must be a supported ISO 4217 code.");
        }
    }
}