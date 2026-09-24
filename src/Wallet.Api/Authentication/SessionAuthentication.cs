namespace Wallet.Api.Authentication
{
    public static class SessionAuthentication
    {
        public const string Scheme = "Session";
        public const string SelectorScheme = "BearerOrSession";
        public const string CookieName = "__Host-wallet_session";
        public const string CsrfHeader = "X-CSRF";
        public const string FamilyClaim = "sid";

        public static readonly PathString EndpointPath = new("/api/v1/auth/session");

        public static bool UsesAuthorizationHeader(HttpRequest request) =>
            request.Headers.Authorization.Count > 0;

        public static CookieOptions CookieOptions(DateTimeOffset? expires) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expires,
            IsEssential = true
        };
    }
}
