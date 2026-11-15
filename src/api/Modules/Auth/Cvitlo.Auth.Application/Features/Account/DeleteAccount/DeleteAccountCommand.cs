using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Auth.Application.Features.Account.DeleteAccount;

public record DeleteAccountCommand(string Subject, string? IdToken) : ICommand<ErrorOr<Deleted>>;
