using FluentValidation;

namespace Wallet.Application.Expenses.CancelRecurringExpense
{
    public class CancelRecurringExpenseCommandValidator : AbstractValidator<CancelRecurringExpenseCommand>
    {
        public CancelRecurringExpenseCommandValidator()
        {
            RuleFor(x => x.RecurringExpenseId).NotEmpty();
        }
    }
}
