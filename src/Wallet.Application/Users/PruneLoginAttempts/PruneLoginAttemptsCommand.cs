using MediatR;

namespace Wallet.Application.Users.PruneLoginAttempts
{
    public record PruneLoginAttemptsCommand(int RetentionDays = 30) : IRequest<int>;
}
