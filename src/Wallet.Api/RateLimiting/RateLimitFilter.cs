using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Wallet.Api.Configuration;
using Wallet.Application.Abstractions.RateLimiting;

namespace Wallet.Api.RateLimiting
{
    public sealed class RateLimitFilter : IAsyncResourceFilter
    {
        private readonly IRateLimiter _limiter;
        private readonly RateLimitingOptions _options;

        public RateLimitFilter(IRateLimiter limiter, IOptions<RateLimitingOptions> options)
        {
            _limiter = limiter;
            _options = options.Value;
        }

        public async Task OnResourceExecutionAsync(
            ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var http = context.HttpContext;
            var endpoint = http.GetEndpoint();

            if (!_options.Enabled || endpoint?.Metadata.GetMetadata<NoRateLimitAttribute>() is not null)
            {
                await next();
                return;
            }

            var policy = _options.Resolve(SelectPolicyName(http, endpoint));
            var decision = await _limiter.TryAcquireAsync(
                RateLimitKey.For(http), policy, http.RequestAborted);

            if (decision.Remaining >= 0)
            {
                http.Response.Headers["X-RateLimit-Limit"] = policy.Capacity.ToString();
                http.Response.Headers["X-RateLimit-Remaining"] = decision.Remaining.ToString();
            }

            if (decision.Allowed)
            {
                await next();
                return;
            }

            http.Response.Headers.RetryAfter = decision.RetryAfterSeconds.ToString();

            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests.",
                Detail = $"Rate limit exceeded. Retry after {decision.RetryAfterSeconds} seconds."
            })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
        }

        private static string SelectPolicyName(HttpContext http, Endpoint? endpoint)
        {
            var attribute = endpoint?.Metadata.GetMetadata<RateLimitAttribute>();

            if (attribute is not null)
            {
                return attribute.Policy;
            }

            if (http.User.Identity?.IsAuthenticated != true)
            {
                return RateLimitingOptions.Anonymous;
            }

            return HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method)
                ? RateLimitingOptions.Authenticated
                : RateLimitingOptions.AuthenticatedWrite;
        }
    }
}
