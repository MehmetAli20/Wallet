using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Expenses.PostRecurringOccurrence;

namespace Wallet.Application.Expenses.ProcessDueRecurringExpenses
{
    public class ProcessDueRecurringExpensesCommandHandler
        : IRequestHandler<ProcessDueRecurringExpensesCommand, int>
    {
        private readonly IRecurringExpenseRepository _recurring;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProcessDueRecurringExpensesCommandHandler> _logger;

        public ProcessDueRecurringExpensesCommandHandler(
            IRecurringExpenseRepository recurring,
            IServiceScopeFactory scopeFactory,
            ILogger<ProcessDueRecurringExpensesCommandHandler> logger)
        {
            _recurring = recurring;
            _scopeFactory = scopeFactory;
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
                    using var scope = _scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                    var expenseId = await sender.Send(
                        new PostRecurringOccurrenceCommand(recurring.Id, occurrence, key),
                        cancellationToken);

                    if (expenseId is null)
                        _logger.LogInformation(
                            "Recurring expense {RecurringExpenseId} was stopped because one of its members left the group.",
                            recurring.Id);
                    else
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
