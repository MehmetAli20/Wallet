using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Wallet.ArchitectureTests
{
    public class ResponseContractTests
    {
        private static readonly Regex LoginIdentifier =
            new("username|email", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        [Fact]
        public void Responses_Should_Not_Expose_Login_Identifiers()
        {
            var offenders = typeof(Program).Assembly
                .GetTypes()
                .Where(type => type.Name.EndsWith("Response", StringComparison.Ordinal))
                .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => LoginIdentifier.IsMatch(property.Name))
                    .Select(property => $"{type.FullName}.{property.Name}"))
                .OrderBy(name => name)
                .ToList();

            string.Join(Environment.NewLine, offenders).Should().BeEmpty(
                "usernames and e-mail addresses are login identifiers; showing them to another user hands out half a credential and lets anyone lock that account out");
        }
    }
}
