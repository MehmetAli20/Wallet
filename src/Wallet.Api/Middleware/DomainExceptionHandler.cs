using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Wallet.Application.Abstractions.Exceptions;
using Wallet.Domain.Exceptions;

namespace Wallet.Api.Middleware
{
    public class DomainExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is FluentValidation.ValidationException validationException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                var problem = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed"
                };

                await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
                return true;
            }
            var (statusCode, title) = exception switch
            {
                AccountNotFoundException => (StatusCodes.Status404NotFound, "Account not found"),
                InsufficientFundsException => (StatusCodes.Status400BadRequest, "Insufficient funds"),
                CurrencyMismatchException => (StatusCodes.Status400BadRequest, "Currency mismatch"),
                InvalidTransferException => (StatusCodes.Status400BadRequest, "Invalid transfer"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid input"),
                ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrent modification"),
                InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Invalid credentials"),
                UsernameAlreadyExistsException => (StatusCodes.Status409Conflict, "Username already exists"),
                UniqueConstraintViolationException => (StatusCodes.Status409Conflict, "Conflict"),
                IdempotentResponseUnavailableException => (StatusCodes.Status409Conflict, "Idempotent response unavailable"),
                EmailAlreadyExistsException => (StatusCodes.Status409Conflict, "Email already exists"),
                _ => (0, string.Empty)
            };
            if(statusCode == 0)
            {
                return false;
            }

            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}