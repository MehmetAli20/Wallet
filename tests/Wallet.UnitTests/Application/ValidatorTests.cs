using FluentAssertions;
using FluentValidation;
using Wallet.Application.Expenses.CreateExpense;
using Wallet.Application.Groups.CreateGroup;
using Wallet.Application.Groups.InviteToGroup;
using Wallet.Application.Transfers.TransferMoney;
using Wallet.Application.Users.Login;
using Wallet.Application.Users.Register;

namespace Wallet.UnitTests.Application
{
    public class ValidatorTests
    {
        private static bool IsValid<T>(IValidator<T> validator, T instance) =>
            validator.Validate(instance).IsValid;

        private static IEnumerable<string> Errors<T>(IValidator<T> validator, T instance) =>
            validator.Validate(instance).Errors.Select(e => e.PropertyName);

        private static CreateGroupCommand ValidGroup() => new("Piknik", "TRY");

        [Fact]
        public void CreateGroup_ValidCommand_Passes() =>
            IsValid(new CreateGroupCommandValidator(), ValidGroup()).Should().BeTrue();

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateGroup_EmptyName_Fails(string name) =>
            Errors(new CreateGroupCommandValidator(), ValidGroup() with { Name = name })
                .Should().Contain(nameof(CreateGroupCommand.Name));

        [Fact]
        public void CreateGroup_TooLongName_Fails() =>
            Errors(new CreateGroupCommandValidator(), ValidGroup() with { Name = new string('a', 101) })
                .Should().Contain(nameof(CreateGroupCommand.Name));

        [Theory]
        [InlineData("")]
        [InlineData("TR")]
        [InlineData("TRYY")]
        [InlineData("TR1")]
        public void CreateGroup_InvalidCurrency_Fails(string currency) =>
            Errors(new CreateGroupCommandValidator(), ValidGroup() with { Currency = currency })
                .Should().Contain(nameof(CreateGroupCommand.Currency));

        [Fact]
        public void CreateGroup_LowercaseCurrency_Passes() =>
            IsValid(new CreateGroupCommandValidator(), ValidGroup() with { Currency = "try" })
                .Should().BeTrue();

        private static InviteToGroupCommand ValidInvite() => new(Guid.NewGuid(), Guid.NewGuid());

        [Fact]
        public void InviteToGroup_ValidCommand_Passes() =>
            IsValid(new InviteToGroupCommandValidator(), ValidInvite()).Should().BeTrue();

        [Fact]
        public void InviteToGroup_EmptyGroupId_Fails() =>
            Errors(new InviteToGroupCommandValidator(), ValidInvite() with { GroupId = Guid.Empty })
                .Should().Contain(nameof(InviteToGroupCommand.GroupId));

        [Fact]
        public void InviteToGroup_EmptyUserId_Fails() =>
            Errors(new InviteToGroupCommandValidator(), ValidInvite() with { UserId = Guid.Empty })
                .Should().Contain(nameof(InviteToGroupCommand.UserId));

        private static TransferMoneyCommand ValidTransfer() =>
            new(Guid.NewGuid(), Guid.NewGuid(), 50m, "key");

        [Fact]
        public void TransferMoney_ValidCommand_Passes() =>
            IsValid(new TransferMoneyCommandValidator(), ValidTransfer()).Should().BeTrue();

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.01)]
        public void TransferMoney_NonPositiveAmount_Fails(decimal amount) =>
            Errors(new TransferMoneyCommandValidator(), ValidTransfer() with { Amount = amount })
                .Should().Contain(nameof(TransferMoneyCommand.Amount));

        [Fact]
        public void TransferMoney_EmptyGroupId_Fails() =>
            Errors(new TransferMoneyCommandValidator(), ValidTransfer() with { GroupId = Guid.Empty })
                .Should().Contain(nameof(TransferMoneyCommand.GroupId));

        [Fact]
        public void TransferMoney_EmptyRecipient_Fails() =>
            Errors(new TransferMoneyCommandValidator(), ValidTransfer() with { RecipientUserId = Guid.Empty })
                .Should().Contain(nameof(TransferMoneyCommand.RecipientUserId));

        [Fact]
        public void TransferMoney_EmptyIdempotencyKey_Fails() =>
            Errors(new TransferMoneyCommandValidator(), ValidTransfer() with { IdempotencyKey = "" })
                .Should().Contain(nameof(TransferMoneyCommand.IdempotencyKey));

        private static CreateExpenseCommand ValidExpense()
        {
            var payer = Guid.NewGuid();

            return new CreateExpenseCommand(
                Guid.NewGuid(),
                payer,
                100m,
                "Market",
                DateTimeOffset.UtcNow,
                new List<Guid> { payer },
                new Dictionary<Guid, decimal>(),
                "key");
        }

        [Fact]
        public void CreateExpense_ValidCommand_Passes() =>
            IsValid(new CreateExpenseCommandValidator(), ValidExpense()).Should().BeTrue();

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void CreateExpense_NonPositiveAmount_Fails(decimal amount) =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { Amount = amount })
                .Should().Contain(nameof(CreateExpenseCommand.Amount));

        [Fact]
        public void CreateExpense_EmptyDescription_Fails() =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { Description = "" })
                .Should().Contain(nameof(CreateExpenseCommand.Description));

        [Fact]
        public void CreateExpense_TooLongDescription_Fails() =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { Description = new string('a', 201) })
                .Should().Contain(nameof(CreateExpenseCommand.Description));

        [Fact]
        public void CreateExpense_NoParticipants_Fails() =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { Participants = new List<Guid>() })
                .Should().Contain(nameof(CreateExpenseCommand.Participants));

        [Fact]
        public void CreateExpense_DefaultOccurredAt_Fails() =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { OccurredAt = default })
                .Should().Contain(nameof(CreateExpenseCommand.OccurredAt));

        private static LoginCommand ValidLogin() => new("ahmet", "password123");

        [Fact]
        public void Login_ValidCommand_Passes() =>
            IsValid(new LoginCommandValidator(), ValidLogin()).Should().BeTrue();

        [Fact]
        public void Login_EmptyUsername_Fails() =>
            Errors(new LoginCommandValidator(), ValidLogin() with { Username = "" })
                .Should().Contain(nameof(LoginCommand.Username));

        [Fact]
        public void Login_TooLongUsername_Fails() =>
            Errors(new LoginCommandValidator(), ValidLogin() with { Username = new string('a', 51) })
                .Should().Contain(nameof(LoginCommand.Username));

        [Fact]
        public void Login_EmptyPassword_Fails() =>
            Errors(new LoginCommandValidator(), ValidLogin() with { Password = "" })
                .Should().Contain(nameof(LoginCommand.Password));

        private static RegisterCommand ValidRegister() => new("ahmet", "ahmet@test.com", "password123");

        [Fact]
        public void Register_ValidCommand_Passes() =>
            IsValid(new RegisterCommandValidator(), ValidRegister()).Should().BeTrue();

        [Theory]
        [InlineData("ab")]
        [InlineData("")]
        [InlineData("ahmet bey")]
        [InlineData("ahmet@")]
        public void Register_InvalidUsername_Fails(string username) =>
            Errors(new RegisterCommandValidator(), ValidRegister() with { Username = username })
                .Should().Contain(nameof(RegisterCommand.Username));

        [Theory]
        [InlineData("ahmet.bey")]
        [InlineData("ahmet_bey")]
        [InlineData("ahmet-bey")]
        [InlineData("ahmet123")]
        public void Register_AllowedUsernameCharacters_Pass(string username) =>
            IsValid(new RegisterCommandValidator(), ValidRegister() with { Username = username })
                .Should().BeTrue();

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        public void Register_InvalidEmail_Fails(string email) =>
            Errors(new RegisterCommandValidator(), ValidRegister() with { Email = email })
                .Should().Contain(nameof(RegisterCommand.Email));

        [Fact]
        public void Register_ShortPassword_Fails() =>
            Errors(new RegisterCommandValidator(), ValidRegister() with { Password = "1234567" })
                .Should().Contain(nameof(RegisterCommand.Password));

        [Fact]
        public void Register_PasswordBeyondBcryptLimit_Fails() =>
            Errors(new RegisterCommandValidator(), ValidRegister() with { Password = new string('a', 73) })
                .Should().Contain(nameof(RegisterCommand.Password));

        [Fact]
        public void Register_PasswordAtBcryptLimit_Passes() =>
            IsValid(new RegisterCommandValidator(), ValidRegister() with { Password = new string('a', 72) })
                .Should().BeTrue();
    }
}