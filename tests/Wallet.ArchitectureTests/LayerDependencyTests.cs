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
    }
}
