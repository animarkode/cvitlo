using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.DeleteCredential;

public record DeleteCredentialCommand(string Subject, string AccessToken, string Id) : ICommand<ErrorOr<Success>>;
