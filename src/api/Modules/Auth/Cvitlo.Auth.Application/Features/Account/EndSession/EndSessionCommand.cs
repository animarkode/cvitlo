using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.EndSession;

public record EndSessionCommand(string AccessToken, string Id) : ICommand<ErrorOr<Success>>;
