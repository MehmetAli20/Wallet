using MediatR;
using Wallet.Application.Users.PruneLoginAttempts;

namespace Wallet.Worker.Jobs
{
    public class LoginAttemptPruneJob
    {
        private readonly ISender _sender;
        private readonly ILogger<LoginAttemptPruneJob> _logger;

        public LoginAttemptPruneJob(ISender sender, ILogger<LoginAttemptPruneJob> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var deleted = await _sender.Send(new PruneLoginAttemptsCommand(), cancellationToken);

            _logger.LogInformation("Pruned {Count} login attempts.", deleted);
        }
    }
}
