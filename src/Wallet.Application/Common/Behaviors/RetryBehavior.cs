using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Common.Behaviors;

public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int MaxAttempts = 2;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;

    public RetryBehavior(IServiceScopeFactory scopeFactory, ILogger<RetryBehavior<TRequest, TResponse>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return attempt == 1
                    ? await next()
                    : await SendFromFreshScope(request, cancellationToken);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(
                    "Concurrency conflict on {RequestName} (attempt {Attempt}/{MaxAttempts}), retrying with fresh state.",
                    typeof(TRequest).Name, attempt, MaxAttempts);
            }
        }

        throw new InvalidOperationException("Retry loop exited without a result.");
    }

    private async Task<TResponse> SendFromFreshScope(TRequest request, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(request, cancellationToken);
        return (TResponse)result!;
    }
}