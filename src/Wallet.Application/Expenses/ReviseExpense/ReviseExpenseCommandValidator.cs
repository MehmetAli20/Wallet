using FluentValidation;

namespace Wallet.Application.Expenses.ReviseExpense
{
    public class ReviseExpenseCommandValidator : AbstractValidator<ReviseExpenseCommand>
    {
        public ReviseExpenseCommandValidator()
        {
            RuleFor(x => x.ExpenseId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OccurredAt).NotEqual(default(DateTimeOffset));
            RuleFor(x => x.Participants).NotEmpty();
            RuleFor(x => x.IdempotencyKey).NotEmpty();
        }
    }
}
