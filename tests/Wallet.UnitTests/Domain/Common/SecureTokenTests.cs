using FluentAssertions;
using Wallet.Domain.Common;

namespace Wallet.UnitTests.Domain.Common
{
    public class SecureTokenTests
    {
        private const string Prefix = "wrt";
        private static readonly string Body = new string('A', 43);

        public static TheoryData<string?> MalformedTokens => new()
        {
            null,
            "",
            $"wli_{Body}",
            $"WRT_{Body}",
            $"wrt{Body}A",
            $"wrt_{Body[..^1]}",
            $"wrt_{Body}A",
            $"wrt_{Body[..^1]}+",
            $"wrt_{Body[..^1]}/",
            $"wrt_{Body[..^1]}=",
            $"wrt_{Body[..^1]}ı",
            $"wrt_{Body[..^1]} "
        };

        [Fact]
        public void New_ProducesDistinctTokensThatPassTheFormatCheck()
        {
            var first = SecureToken.New(Prefix);
            var second = SecureToken.New(Prefix);

            SecureToken.HasFormat(first, Prefix).Should().BeTrue();
            SecureToken.HasFormat(second, Prefix).Should().BeTrue();
            first.Should().NotBe(second);
        }

        [Fact]
        public void TheReferenceShape_PassesTheFormatCheck()
        {
            SecureToken.HasFormat($"wrt_{Body}", Prefix).Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(MalformedTokens))]
        public void HasFormat_RejectsMalformedTokens(string? token)
        {
            SecureToken.HasFormat(token, Prefix).Should().BeFalse();
        }
    }
}
