namespace Cvitlo.Auth.Application.Features.Account.DeleteAccount;

public static class DeleteAccountMapper
{
    public static DeleteAccountCommand MapToCommand(string subject, string? idToken)
    {
        return new DeleteAccountCommand(subject, idToken);
    }
}
