using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wallet.Api.Configuration;

namespace Wallet.IntegrationTests.Api
{
    public class ForwardedHeadersTests
    {
        private static async Task<string> SeenIpAsync(
            ForwardingOptions forwarding, string connectionFrom, string? forwardedFor)
        {
            using var host = await new HostBuilder()
                .ConfigureWebHost(web =>
                {
                    web.UseTestServer();
                    web.ConfigureServices(services =>
                        services.Configure<ForwardedHeadersOptions>(forwarding.ApplyTo));
                    web.Configure(app =>
                    {
                        app.Use(async (context, next) =>
                        {
                            context.Connection.RemoteIpAddress =
                                System.Net.IPAddress.Parse(connectionFrom);

                            await next();
                        });

                        if (forwarding.Enabled)
                            app.UseForwardedHeaders();

                        app.Run(context =>
                            context.Response.WriteAsync(
                                context.Connection.RemoteIpAddress!.ToString()));
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();

            if (forwardedFor is not null)
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);

            return await client.GetStringAsync("/");
        }

        private static ForwardingOptions BehindIngress() => new()
        {
            Enabled = true,
            ForwardLimit = 1,
            KnownNetworks = ["10.244.0.0/16"]
        };

        [Fact]
        public async Task BehindATrustedProxy_TheClientIpIsRecovered()
        {
            var seen = await SeenIpAsync(
                BehindIngress(), connectionFrom: "10.244.0.15", forwardedFor: "85.100.20.7");

            seen.Should().Be("85.100.20.7");
        }

        [Fact]
        public async Task AHeaderSentThroughTheProxy_CannotReachPastTheForwardLimit()
        {
            var seen = await SeenIpAsync(
                BehindIngress(),
                connectionFrom: "10.244.0.15",
                forwardedFor: "9.9.9.9, 85.100.20.7");

            seen.Should().Be("85.100.20.7");
            seen.Should().NotBe("9.9.9.9");
        }

        [Fact]
        public async Task FromAnUntrustedAddress_TheHeaderIsIgnored()
        {
            var seen = await SeenIpAsync(
                BehindIngress(), connectionFrom: "1.2.3.4", forwardedFor: "9.9.9.9");

            seen.Should().Be("1.2.3.4");
        }

        [Fact]
        public void EnablingForwardingWithNothingTrusted_FailsAtStartup()
        {
            var forwarding = new ForwardingOptions { Enabled = true, ForwardLimit = 1 };

            var act = () => forwarding.ApplyTo(new ForwardedHeadersOptions());

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*no proxy is trusted*");
        }

        [Fact]
        public async Task WhenDisabled_TheHeaderIsNeverRead()
        {
            var forwarding = new ForwardingOptions
            {
                Enabled = false,
                KnownNetworks = ["10.244.0.0/16"]
            };

            var seen = await SeenIpAsync(
                forwarding, connectionFrom: "10.244.0.15", forwardedFor: "85.100.20.7");

            seen.Should().Be("10.244.0.15");
        }

        [Fact]
        public async Task ANodeSliceInsteadOfTheClusterRange_StopsTrustingOtherNodes()
        {
            var nodeSliceOnly = new ForwardingOptions
            {
                Enabled = true,
                ForwardLimit = 1,
                KnownNetworks = ["10.244.0.0/24"]
            };

            var sameNode = await SeenIpAsync(
                nodeSliceOnly, connectionFrom: "10.244.0.15", forwardedFor: "85.100.20.7");

            var otherNode = await SeenIpAsync(
                nodeSliceOnly, connectionFrom: "10.244.1.15", forwardedFor: "85.100.20.7");

            sameNode.Should().Be("85.100.20.7");
            otherNode.Should().Be("10.244.1.15");
        }
    }
}
