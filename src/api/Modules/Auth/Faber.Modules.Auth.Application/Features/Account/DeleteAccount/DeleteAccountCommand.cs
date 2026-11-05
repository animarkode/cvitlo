using ErrorOr;
using FastEndpoints;

namespace Faber.Modules.Auth.Application.Features.Account.DeleteAccount;

/// <param name="Subject">The signed-in user's <c>sub</c>, taken from the session and never from the request.</param>
/// <param name="IdToken">The session's stored ID token, which carries the time of the last sign-in.</param>
public record DeleteAccountCommand(string Subject, string? IdToken) : ICommand<ErrorOr<Deleted>>;
