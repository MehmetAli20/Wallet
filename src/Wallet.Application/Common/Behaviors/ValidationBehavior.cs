using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)))).SelectMany(result => result.Errors).ToList();

                if(failures.Count!=0)
                {
                    throw new ValidationException(failures);
                }
            }
            return await next();
        }
    }
}