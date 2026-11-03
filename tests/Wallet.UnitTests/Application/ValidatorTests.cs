using FluentAssertions;
using FluentValidation;
using Wallet.Application.Abstractions;
using Wallet.Application.Expenses.CreateExpense;
using Wallet.Application.Expenses.CreateRecurringExpense;
using Wallet.Application.Expenses.ReverseExpense;
using Wallet.Application.Expenses.ReviseExpense;
using Wallet.Application.Groups.ClaimPlaceholder;
using Wallet.Application.Groups.CreateGroup;
using Wallet.Application.Groups.EnsurePair;
using Wallet.Application.Groups.InviteToGroup;
using Wallet.Application.Transfers.TransferMoney;
using Wallet.Application.Users.BlockUser;
using Wallet.Application.Users.DeleteCurrentUser;
using Wallet.Application.Users.Login;
using Wallet.Application.Users.Register;
using Wallet.Application.Users.RenameCurrentUser;
using Wallet.Application.Users.UnblockUser;
using Wallet.Application.Invitations.AcceptInvitation;
using Wallet.Application.Invitations.DeclineInvitation;
using Wallet.Domain.Common;
using Wallet.Domain.Expenses;

namespace Wallet.UnitTests.Application
{
    public class ValidatorTests
    {
        private static bool IsValid<T>(IValidator<T> validator, T instance) =>
            validator.Validate(instance).IsValid;

        private static IEnumerable<string> Errors<T>(IValidator<T> validator, T instance) =>
            validator.Validate(instance).Errors.Select(e => e.PropertyName);

        private static readonly string TooLongKey = new('k', IIdempotentRequest.KeyMaxLength + 1);

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

        [Theory]
        [InlineData("ABC")]
        [InlineData("XAU")]
        public void CreateGroup_UnsupportedCurrency_Fails(string currency) =>
            Errors(new CreateGroupCommandValidator(), ValidGroup() with { Currency = currency })
                .Should().Contain(nameof(CreateGroupCommand.Currency));

        private static EnsurePairCommand ValidPair() => new(Guid.NewGuid(), "USD");

        [Fact]
        public void EnsurePair_ValidCommand_Passes() =>
            IsValid(new EnsurePairCommandValidator(), ValidPair()).Should().BeTrue();

        [Theory]
        [InlineData("ABC")]
        [InlineData("XAU")]
        [InlineData("US1")]
        public void EnsurePair_UnsupportedCurrency_Fails(string currency) =>
            Errors(new EnsurePairCommandValidator(), ValidPair() with { Currency = currency })
                .Should().Contain(nameof(EnsurePairCommand.Currency));

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

        [Fact]
        public void TransferMoney_TooLongIdempotencyKey_Fails() =>
            Errors(new TransferMoneyCommandValidator(), ValidTransfer() with { IdempotencyKey = TooLongKey })
                .Should().Contain(nameof(TransferMoneyCommand.IdempotencyKey));

        [Fact]
        public void TransferMoney_AmountAboveTheMaximum_Fails() =>
            Errors(new TransferMoneyCommandValidator(), ValidTransfer() with { Amount = Money.MaxAmount + 0.01m })
                .Should().Contain(nameof(TransferMoneyCommand.Amount));

        [Fact]
        public void TransferMoney_SubCentAmount_Fails() =>
            Errors(new TransferMoneyCommandValidator(), ValidTransfer() with { Amount = 10.005m })
                .Should().Contain(nameof(TransferMoneyCommand.Amount));

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

        [Fact]
        public void CreateExpense_TooLongIdempotencyKey_Fails() =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { IdempotencyKey = TooLongKey })
                .Should().Contain(nameof(CreateExpenseCommand.IdempotencyKey));

        [Fact]
        public void CreateExpense_IdempotencyKeyAtTheLimit_Passes() =>
            IsValid(new CreateExpenseCommandValidator(),
                    ValidExpense() with { IdempotencyKey = new string('k', IIdempotentRequest.KeyMaxLength) })
                .Should().BeTrue();

        [Fact]
        public void CreateExpense_TooManyParticipants_Fails() =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with
                {
                    Participants = Enumerable.Range(0, Expense.MaxParticipants + 1).Select(_ => Guid.NewGuid()).ToList()
                })
                .Should().Contain(nameof(CreateExpenseCommand.Participants));

        [Fact]
        public void CreateExpense_RepeatedParticipant_Fails()
        {
            var someone = Guid.NewGuid();

            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { Participants = new List<Guid> { someone, someone } })
                .Should().Contain(nameof(CreateExpenseCommand.Participants));
        }

        [Fact]
        public void CreateExpense_AmountAboveTheMaximum_Fails() =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { Amount = Money.MaxAmount + 0.01m })
                .Should().Contain(nameof(CreateExpenseCommand.Amount));

        [Fact]
        public void CreateExpense_AmountAtTheMaximum_Passes() =>
            IsValid(new CreateExpenseCommandValidator(), ValidExpense() with { Amount = Money.MaxAmount })
                .Should().BeTrue();

        [Fact]
        public void CreateExpense_SubCentAmount_Fails() =>
            Errors(new CreateExpenseCommandValidator(), ValidExpense() with { Amount = 10.005m })
                .Should().Contain(nameof(CreateExpenseCommand.Amount));

        [Fact]
        public void CreateExpense_SubCentShare_Fails()
        {
            var expense = ValidExpense();

            Errors(new CreateExpenseCommandValidator(), expense with
                {
                    FixedShares = new Dictionary<Guid, decimal> { [expense.PayerId] = 33.335m }
                })
                .Should().Contain(nameof(CreateExpenseCommand.FixedShares));
        }

        [Fact]
        public void CreateExpense_ShareAboveTheMaximum_Fails()
        {
            var expense = ValidExpense();

            Errors(new CreateExpenseCommandValidator(), expense with
                {
                    FixedShares = new Dictionary<Guid, decimal> { [expense.PayerId] = Money.MaxAmount + 0.01m }
                })
                .Should().Contain(nameof(CreateExpenseCommand.FixedShares));
        }

        private static ReviseExpenseCommand ValidRevision() =>
            new(Guid.NewGuid(), 100m, "Market", DateTimeOffset.UtcNow,
                new List<Guid> { Guid.NewGuid() }, new Dictionary<Guid, decimal>(), "key");

        [Fact]
        public void ReviseExpense_ValidCommand_Passes() =>
            IsValid(new ReviseExpenseCommandValidator(), ValidRevision()).Should().BeTrue();

        [Fact]
        public void ReviseExpense_TooLongIdempotencyKey_Fails() =>
            Errors(new ReviseExpenseCommandValidator(), ValidRevision() with { IdempotencyKey = TooLongKey })
                .Should().Contain(nameof(ReviseExpenseCommand.IdempotencyKey));

        [Fact]
        public void ReviseExpense_AmountAboveTheMaximum_Fails() =>
            Errors(new ReviseExpenseCommandValidator(), ValidRevision() with { Amount = Money.MaxAmount + 0.01m })
                .Should().Contain(nameof(ReviseExpenseCommand.Amount));

        [Fact]
        public void ReviseExpense_SubCentShare_Fails()
        {
            var revision = ValidRevision();

            Errors(new ReviseExpenseCommandValidator(), revision with
                {
                    FixedShares = new Dictionary<Guid, decimal> { [revision.Participants[0]] = 33.335m }
                })
                .Should().Contain(nameof(ReviseExpenseCommand.FixedShares));
        }

        private static ReverseExpenseCommand ValidReversal() => new(Guid.NewGuid(), null, "key");

        [Fact]
        public void ReverseExpense_ValidCommand_Passes() =>
            IsValid(new ReverseExpenseCommandValidator(), ValidReversal()).Should().BeTrue();

        [Fact]
        public void ReverseExpense_TooLongIdempotencyKey_Fails() =>
            Errors(new ReverseExpenseCommandValidator(), ValidReversal() with { IdempotencyKey = TooLongKey })
                .Should().Contain(nameof(ReverseExpenseCommand.IdempotencyKey));

        private static CreateRecurringExpenseCommand ValidRecurring()
        {
            var payer = Guid.NewGuid();

            return new CreateRecurringExpenseCommand(
                Guid.NewGuid(), payer, 100m, "Rent", RecurrenceInterval.Monthly, DateTimeOffset.UtcNow,
                new List<Guid> { payer }, new Dictionary<Guid, decimal>(), "key");
        }

        [Fact]
        public void CreateRecurringExpense_ValidCommand_Passes() =>
            IsValid(new CreateRecurringExpenseCommandValidator(), ValidRecurring()).Should().BeTrue();

        [Fact]
        public void CreateRecurringExpense_TooLongIdempotencyKey_Fails() =>
            Errors(new CreateRecurringExpenseCommandValidator(), ValidRecurring() with { IdempotencyKey = TooLongKey })
                .Should().Contain(nameof(CreateRecurringExpenseCommand.IdempotencyKey));

        [Fact]
        public void CreateRecurringExpense_AmountAboveTheMaximum_Fails() =>
            Errors(new CreateRecurringExpenseCommandValidator(), ValidRecurring() with { Amount = Money.MaxAmount + 0.01m })
                .Should().Contain(nameof(CreateRecurringExpenseCommand.Amount));

        [Fact]
        public void CreateRecurringExpense_SubCentShare_Fails()
        {
            var recurring = ValidRecurring();

            Errors(new CreateRecurringExpenseCommandValidator(), recurring with
                {
                    FixedShares = new Dictionary<Guid, decimal> { [recurring.PayerId] = 33.335m }
                })
                .Should().Contain(nameof(CreateRecurringExpenseCommand.FixedShares));
        }

        [Fact]
        public void CreateRecurringExpense_FirstOccurrenceEarlierToday_Passes() =>
            IsValid(new CreateRecurringExpenseCommandValidator(), ValidRecurring() with { FirstOccurrence = DateTimeOffset.UtcNow.AddHours(-23) })
                .Should().BeTrue();

        [Fact]
        public void CreateRecurringExpense_FirstOccurrenceMoreThanADayAgo_Fails() =>
            Errors(new CreateRecurringExpenseCommandValidator(), ValidRecurring() with { FirstOccurrence = DateTimeOffset.UtcNow.AddDays(-2) })
                .Should().Contain(nameof(CreateRecurringExpenseCommand.FirstOccurrence));

        [Fact]
        public void CreateRecurringExpense_FirstOccurrenceMoreThanAYearAhead_Fails() =>
            Errors(new CreateRecurringExpenseCommandValidator(), ValidRecurring() with { FirstOccurrence = DateTimeOffset.UtcNow.AddYears(1).AddDays(1) })
                .Should().Contain(nameof(CreateRecurringExpenseCommand.FirstOccurrence));

        private static LoginCommand ValidLogin() => new("ahmet@test.com", "password123");

        [Fact]
        public void Login_ValidCommand_Passes() =>
            IsValid(new LoginCommandValidator(), ValidLogin()).Should().BeTrue();

        [Fact]
        public void Login_EmptyEmail_Fails() =>
            Errors(new LoginCommandValidator(), ValidLogin() with { Email = "" })
                .Should().Contain(nameof(LoginCommand.Email));

        [Fact]
        public void Login_TooLongEmail_Fails() =>
            Errors(new LoginCommandValidator(), ValidLogin() with { Email = new string('a', 255) })
                .Should().Contain(nameof(LoginCommand.Email));

        [Fact]
        public void Login_EmptyPassword_Fails() =>
            Errors(new LoginCommandValidator(), ValidLogin() with { Password = "" })
                .Should().Contain(nameof(LoginCommand.Password));

        private static RegisterCommand ValidRegister() => new("ahmet@test.com", "password123", "Ahmet Yılmaz");

        [Fact]
        public void Register_ValidCommand_Passes() =>
            IsValid(new RegisterCommandValidator(), ValidRegister()).Should().BeTrue();

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

        [Fact]
        public void Register_PasswordBeyondBcryptLimitInBytes_Fails() =>
            Errors(new RegisterCommandValidator(), ValidRegister() with { Password = new string('ş', 37) })
                .Should().Contain(nameof(RegisterCommand.Password));

        private static ClaimPlaceholderCommand ValidClaim() => new("token", "ahmet@test.com", "password123");

        [Fact]
        public void ClaimPlaceholder_ValidCommand_Passes() =>
            IsValid(new ClaimPlaceholderCommandValidator(), ValidClaim()).Should().BeTrue();

        [Fact]
        public void ClaimPlaceholder_PasswordBeyondBcryptLimitInBytes_Fails() =>
            Errors(new ClaimPlaceholderCommandValidator(), ValidClaim() with { Password = new string('ş', 37) })
                .Should().Contain(nameof(ClaimPlaceholderCommand.Password));

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("‮temhA")]
        public void Register_InvalidDisplayName_Fails(string displayName) =>
            Errors(new RegisterCommandValidator(), ValidRegister() with { DisplayName = displayName })
                .Should().Contain(nameof(RegisterCommand.DisplayName));

        [Fact]
        public void AcceptInvitation_ValidCommand_Passes() =>
            new AcceptInvitationCommandValidator()
                .Validate(new AcceptInvitationCommand(Guid.NewGuid()))
                .IsValid.Should().BeTrue();

        [Fact]
        public void AcceptInvitation_EmptyId_Fails() =>
            new AcceptInvitationCommandValidator()
                .Validate(new AcceptInvitationCommand(Guid.Empty))
                .IsValid.Should().BeFalse();

        [Fact]
        public void DeclineInvitation_ValidCommand_Passes() =>
            new DeclineInvitationCommandValidator()
                .Validate(new DeclineInvitationCommand(Guid.NewGuid()))
                .IsValid.Should().BeTrue();

        [Fact]
        public void DeclineInvitation_EmptyId_Fails() =>
            new DeclineInvitationCommandValidator()
                .Validate(new DeclineInvitationCommand(Guid.Empty))
                .IsValid.Should().BeFalse();

        [Fact]
        public void RenameCurrentUser_ValidName_Passes() =>
            IsValid(new RenameCurrentUserCommandValidator(), new RenameCurrentUserCommand("Ayşe Yılmaz")).Should().BeTrue();

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("‮eşyA")]
        public void RenameCurrentUser_InvalidName_Fails(string displayName) =>
            Errors(new RenameCurrentUserCommandValidator(), new RenameCurrentUserCommand(displayName))
                .Should().Contain(nameof(RenameCurrentUserCommand.DisplayName));

        [Fact]
        public void DeleteCurrentUser_WithoutPassword_Fails() =>
            Errors(new DeleteCurrentUserCommandValidator(), new DeleteCurrentUserCommand(""))
                .Should().Contain(nameof(DeleteCurrentUserCommand.Password));

        [Fact]
        public void BlockUser_EmptyId_Fails() =>
            Errors(new BlockUserCommandValidator(), new BlockUserCommand(Guid.Empty))
                .Should().Contain(nameof(BlockUserCommand.UserId));

        [Fact]
        public void UnblockUser_EmptyId_Fails() =>
            Errors(new UnblockUserCommandValidator(), new UnblockUserCommand(Guid.Empty))
                .Should().Contain(nameof(UnblockUserCommand.UserId));
    }
}