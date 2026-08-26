using FluentValidation;

namespace Wallet.Application.Expenses.CreateExpense
{
    public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
    {
        public CreateExpenseCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.PayerId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OccurredAt).NotEqual(default(DateTimeOffset));
            RuleFor(x => x.Participants).NotEmpty();
            RuleFor(x => x.IdempotencyKey).NotEmpty();
        }
    }
}