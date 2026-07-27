using FluentAssertions;
using NetArchTest.Rules;

namespace Wallet.ArchitectureTests
{
    public class LayerDependencyTests
    {
        [Fact]
        public void Domain_Should_Not_Depend_On_Outer_Layers()
        {
            var result = Types.InAssembly(typeof(Wallet.Domain.AssemblyMarker).Assembly)
                .ShouldNot()
                .HaveDependencyOnAny("Wallet.Application", "Wallet.Infrastructure", "Wallet.Api")
                .GetResult();
            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Domain_Should_Not_Depend_On_Infrastructure_Concerns()
        {
            var result = Types.InAssembly(typeof(Wallet.Domain.AssemblyMarker).Assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.EntityFrameworkCore",
                    "Npgsql",
                    "Microsoft.AspNetCore",
                    "System.Data")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "ihlal eden tipler: {0}",
                string.Join(", ", result.FailingTypeNames ?? []));
        }

        [Fact]
        public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
        {
            var result = Types.InAssembly(typeof(Wallet.Application.AssemblyMarker).Assembly)
                .ShouldNot()
                .HaveDependencyOnAny("Wallet.Infrastructure", "Wallet.Api")
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Violating types: {0}", string.Join(", ",result.FailingTypeNames ?? []));
        }
    }
}