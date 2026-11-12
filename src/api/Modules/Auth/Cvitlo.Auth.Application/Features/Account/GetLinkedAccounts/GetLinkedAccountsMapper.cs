using Cvitlo.Auth.Application.Features.AccountAction;
using Cvitlo.Auth.Application.Sessions;

namespace Cvitlo.Auth.Application.Features.Account.GetLinkedAccounts;

public static class GetLinkedAccountsMapper
{
    public static GetLinkedAccountsResponse MapToResponse(this KeycloakLinkedAccount account)
    {
        return new GetLinkedAccountsResponse(
            account.ProviderAlias,
            account.DisplayName,
            account.Connected,
            account.LinkedUsername,
            AccountActions.FindName($"idp_link:{account.ProviderAlias}"));
    }
}
