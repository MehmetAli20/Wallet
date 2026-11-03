namespace Wallet.Api.Contracts.Users
{
    public record DeleteCurrentUserRequest(string Password)
    {
        public sealed override string ToString() =>
            $"{nameof(DeleteCurrentUserRequest)} {{ Password = *** }}";
    }
}
