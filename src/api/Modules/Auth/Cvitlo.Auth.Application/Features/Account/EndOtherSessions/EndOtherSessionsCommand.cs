using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.EndOtherSessions;

public record EndOtherSessionsCommand(string AccessToken) : ICommand<ErrorOr<Success>>;
