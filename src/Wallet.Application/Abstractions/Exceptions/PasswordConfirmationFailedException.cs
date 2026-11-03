namespace Wallet.Application.Abstractions.Exceptions
{
    public class PasswordConfirmationFailedException : Exception
    {
        public PasswordConfirmationFailedException() : base("The password is incorrect.")
        {
        }
    }
}
