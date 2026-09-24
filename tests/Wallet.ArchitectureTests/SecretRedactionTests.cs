using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Wallet.ArchitectureTests
{
    public class SecretRedactionTests
    {
        private static readonly Regex SensitiveName =
            new("password|token|secret", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Assembly[] Assemblies =
        {
            typeof(Wallet.Domain.AssemblyMarker).Assembly,
            typeof(Wallet.Application.AssemblyMarker).Assembly,
            typeof(Program).Assembly
        };

        [Fact]
        public void Records_Carrying_Secrets_Should_Not_Use_The_Compiler_Generated_ToString()
        {
            var offenders = Assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(IsRecord)
                .Where(CarriesSecret)
                .Where(UsesCompilerGeneratedToString)
                .Select(type => type.FullName)
                .OrderBy(name => name)
                .ToList();

            string.Join(Environment.NewLine, offenders).Should().BeEmpty(
                "a record's synthesized ToString prints every member, so formatting one of these into a log line would leak a secret");
        }

        private static bool IsRecord(Type type) =>
            type.IsClass && type.GetMethod("<Clone>$") is not null;

        private static bool CarriesSecret(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(property => SensitiveName.IsMatch(property.Name)
                              && !property.Name.EndsWith("Hash", StringComparison.Ordinal));

        private static bool UsesCompilerGeneratedToString(Type type) =>
            type.GetMethod(
                    nameof(ToString),
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    Type.EmptyTypes)
                ?.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
            ?? true;
    }
}
