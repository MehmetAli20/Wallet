using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Wallet.IntegrationTests.Api
{
    public sealed class ClientIpApiFixture : ApiFixture
    {
        public const string ClientIpHeader = "X-Test-Client-Ip";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter, ClientIpFromHeader>());
        }

        private sealed class ClientIpFromHeader : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Headers.TryGetValue(ClientIpHeader, out var clientIp))
                        context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp.ToString());

                    await nextMiddleware();
                });

                next(app);
            };
        }
    }
}
