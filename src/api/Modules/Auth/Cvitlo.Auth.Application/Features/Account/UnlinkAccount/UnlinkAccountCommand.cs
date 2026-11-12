using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.UnlinkAccount;

public record UnlinkAccountCommand(string Subject, string AccessToken, string Alias) : ICommand<ErrorOr<Success>>;
