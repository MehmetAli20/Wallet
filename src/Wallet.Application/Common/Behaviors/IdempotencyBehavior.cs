using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;

namespace Wallet.Application.Common.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentRequest
{
    private readonly IIdempotencyStore _store;

    public IdempotencyBehavior(IIdempotencyStore store)
    {
        _store = store;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (await _store.ExistsAsync(request.IdempotencyKey, cancellationToken))
        {
            return default!;
        }

        _store.Stage(request.IdempotencyKey, typeof(TRequest).Name);

        return await next();
    }
}