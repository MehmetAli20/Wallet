using FluentAssertions;
using FluentValidation;
using Wallet.Application.Common.Behaviors;

namespace Wallet.UnitTests.Application
{
    public class ValidationBehaviorTests
    {
        private record FakeRequest(string Value);

        private class FakeValidator : AbstractValidator<FakeRequest>
        {
            public FakeValidator() => RuleFor(x => x.Value).NotEmpty();
        }

        [Fact]
        public async Task InvalidRequest_ShortCircuits_WithoutCallingTheHandler()
        {
            var called = false;

            var behavior = new ValidationBehavior<FakeRequest, string>(new[] { new FakeValidator() });

            var act = async () => await behavior.Handle(
                new FakeRequest(""),
                _ => { called = true; return Task.FromResult("handled"); },
                CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
            called.Should().BeFalse();
        }

        [Fact]
        public async Task ValidRequest_ReachesTheHandler()
        {
            var behavior = new ValidationBehavior<FakeRequest, string>(new[] { new FakeValidator() });

            var result = await behavior.Handle(
                new FakeRequest("ok"),
                _ => Task.FromResult("handled"),
                CancellationToken.None);

            result.Should().Be("handled");
        }

        [Fact]
        public async Task NoValidators_LetsTheRequestThrough()
        {
            var behavior = new ValidationBehavior<FakeRequest, string>(Array.Empty<IValidator<FakeRequest>>());

            var result = await behavior.Handle(
                new FakeRequest(""),
                _ => Task.FromResult("handled"),
                CancellationToken.None);

            result.Should().Be("handled");
        }

        [Fact]
        public async Task AllFailures_AreCollected_NotJustTheFirst()
        {
            var validator = new InlineValidator<FakeRequest>();
            validator.RuleFor(x => x.Value).NotEmpty();
            validator.RuleFor(x => x.Value).MinimumLength(5);

            var behavior = new ValidationBehavior<FakeRequest, string>(new[] { validator });

            var act = async () => await behavior.Handle(
                new FakeRequest(""),
                _ => Task.FromResult("handled"),
                CancellationToken.None);

            var thrown = await act.Should().ThrowAsync<ValidationException>();
            thrown.Which.Errors.Should().HaveCountGreaterThan(1);
        }
    }
}