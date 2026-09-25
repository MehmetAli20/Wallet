using MediatR;
using Wallet.Application.Expenses.ProcessDueRecurringExpenses;

namespace Wallet.Worker.Jobs
{
    public class RecurringExpensesJob
    {
        private readonly ISender _sender;
        private readonly ILogger<RecurringExpensesJob> _logger;

        public RecurringExpensesJob(ISender sender, ILogger<RecurringExpensesJob> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            var posted = await _sender.Send(
                new ProcessDueRecurringExpensesCommand(DateTimeOffset.UtcNow));

            if (posted > 0)
                _logger.LogInformation("Posted {Count} recurring expenses.", posted);
        }
    }
}
