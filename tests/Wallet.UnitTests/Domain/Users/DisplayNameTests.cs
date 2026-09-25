using System.Globalization;
using FluentAssertions;
using Wallet.Domain.Users;

namespace Wallet.UnitTests.Domain.Users
{
    public class DisplayNameTests
    {
        [Theory]
        [InlineData("  Ali   Veli  ", "Ali Veli")]
        [InlineData("Ali Veli", "Ali Veli")]
        [InlineData("Ali　Veli", "Ali Veli")]
        [InlineData("Ａｌｉ", "Ali")]
        [InlineData("Çağrı Işık", "Çağrı Işık")]
        [InlineData("Ali 🙂", "Ali 🙂")]
        public void AValidName_IsNormalized(string input, string expected)
        {
            User.NormalizeDisplayName(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Ali\tVeli")]
        [InlineData("Ali\nVeli")]
        [InlineData("Ali​Veli")]
        [InlineData("Ali‍Veli")]
        [InlineData("Ali­Veli")]
        [InlineData("‮ilA")]
        [InlineData("Ali")]
        public void AnInvalidName_IsRejected(string input)
        {
            User.TryNormalizeDisplayName(input, out _).Should().BeFalse();
        }

        [Fact]
        public void NoName_IsRejected()
        {
            User.TryNormalizeDisplayName(null, out _).Should().BeFalse();
        }

        [Fact]
        public void BrokenUtf16_IsRejected()
        {
            User.TryNormalizeDisplayName("Ali" + (char)0xD800, out _).Should().BeFalse();
        }

        [Fact]
        public void InvisibleTagCharacters_AreRejected()
        {
            User.TryNormalizeDisplayName("Ali" + char.ConvertFromUtf32(0xE0041), out _).Should().BeFalse();
        }

        [Fact]
        public void TheLengthLimit_IsInclusive()
        {
            User.TryNormalizeDisplayName(new string('a', User.DisplayNameMaxLength), out _).Should().BeTrue();
            User.TryNormalizeDisplayName(new string('a', User.DisplayNameMaxLength + 1), out _).Should().BeFalse();
        }

        [Fact]
        public void TheComparisonKey_DoesNotDependOnTheCurrentCulture()
        {
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            try
            {
                User.DisplayNameKey("ILKER").Should().Be("ilker");
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void EveryWayOfSettingAName_UsesTheSameRule()
        {
            var registered = () => new User(Guid.NewGuid(), "ali", "ali@test.com", "hash", UserRole.User, "‮ilA");
            var placeholder = () => User.CreatePlaceholder(Guid.NewGuid(), "‮ilA");
            var rename = () => User.CreatePlaceholder(Guid.NewGuid(), "Ali").Rename("‮ilA");

            registered.Should().Throw<ArgumentException>();
            placeholder.Should().Throw<ArgumentException>();
            rename.Should().Throw<ArgumentException>();
        }
    }
}
