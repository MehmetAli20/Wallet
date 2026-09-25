using MediatR;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Expenses.PostRecurringOccurrence;

namespace Wallet.Application.Expenses.ProcessDueRecurringExpenses
{
    public class ProcessDueRecurringExpensesCommandHandler
        : IRequestHandler<ProcessDueRecurringExpensesCommand, int>
    {
        private readonly IRecurringExpenseRepository _recurring;
        private readonly ISender _sender;
        private readonly ILogger<ProcessDueRecurringExpensesCommandHandler> _logger;

        public ProcessDueRecurringExpensesCommandHandler(
            IRecurringExpenseRepository recurring,
            ISender sender,
            ILogger<ProcessDueRecurringExpensesCommandHandler> logger)
        {
            _recurring = recurring;
            _sender = sender;
            _logger = logger;
        }

        public async Task<int> Handle(
            ProcessDueRecurringExpensesCommand request, CancellationToken cancellationToken)
        {
            var due = await _recurring.GetDueAsync(request.AsOf, cancellationToken);
            var posted = 0;

            foreach (var recurring in due)
            {
                var occurrence = recurring.NextOccurrence;
                var key = $"recurring:{recurring.Id:N}:{occurrence:yyyyMMdd}";

                try
                {
                    await _sender.Send(
                        new PostRecurringOccurrenceCommand(recurring.Id, occurrence, key),
                        cancellationToken);

                    posted++;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Recurring expense {RecurringExpenseId} due {Occurrence} could not be posted.",
                        recurring.Id,
                        occurrence);
                }
            }

            return posted;
        }
    }
}
