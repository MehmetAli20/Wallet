using Microsoft.AspNetCore.Mvc;

namespace Wallet.Api.Authentication
{
    public sealed class CsrfProtectionMiddleware
    {
        private readonly RequestDelegate _next;

        public CsrfProtectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (RequiresCsrfHeader(context.Request)
                && context.Request.Headers[SessionAuthentication.CsrfHeader] != "1")
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Missing CSRF header",
                    Detail = $"Requests that change state with a session cookie must send {SessionAuthentication.CsrfHeader}: 1."
                });

                return;
            }

            await _next(context);
        }

        private static bool RequiresCsrfHeader(HttpRequest request)
        {
            if (HttpMethods.IsGet(request.Method)
                || HttpMethods.IsHead(request.Method)
                || HttpMethods.IsOptions(request.Method)
                || HttpMethods.IsTrace(request.Method))
                return false;

            if (SessionAuthentication.UsesAuthorizationHeader(request))
                return false;

            return request.Cookies.ContainsKey(SessionAuthentication.CookieName)
                || request.Path.StartsWithSegments(SessionAuthentication.EndpointPath);
        }
    }
}
