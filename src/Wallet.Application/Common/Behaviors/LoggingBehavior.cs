using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Wallet.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest,TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var stopWatch = Stopwatch.StartNew();

            _logger.LogInformation("Handling {RequestName}", requestName);
            try
            {
                var response = await next();
                _logger.LogInformation("Handled {RequestName} in {ElapsadTime}ms", requestName, stopWatch.ElapsedMilliseconds);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{RequestName} failed after {ElapsedTime}ms", requestName, stopWatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}