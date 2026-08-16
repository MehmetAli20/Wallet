using System.Text.Json;
using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Exceptions;

namespace Wallet.Application.Common.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentRequest
{
    private readonly IIdempotencyStore _store;
    private readonly IUnitOfWork _unitOfWork;

    public IdempotencyBehavior(IIdempotencyStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var lookup = await _store.FindAsync(request.IdempotencyKey, cancellationToken);

        if (lookup.Exists)
        {
            if (lookup.Response is null)
            {
                throw new IdempotentResponseUnavailableException(request.IdempotencyKey);
            }

            return JsonSerializer.Deserialize<TResponse>(lookup.Response)!;
        }

        _store.Stage(request.IdempotencyKey, typeof(TRequest).Name);

        var response = await next();

        await _store.SetResponseAsync(request.IdempotencyKey, JsonSerializer.Serialize(response), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
