using Cvitlo.Auth.PublicApi.Events;
using Cvitlo.Resumes.Application.Features.Resumes.DeleteUserResumes;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Auth.Application.IntegrationTests.Support;

/// <summary>
/// Stands in for the Resumes module's <see cref="UserDeletedHandler"/> at the module boundary. It behaves like the
/// real handler, except that it throws for the subjects a test lists in <see cref="FailFor"/>, so no other test is
/// affected.
/// </summary>
public sealed class FailingUserDeletedHandler(IServiceScopeFactory scopes, ILogger<UserDeletedHandler> logger)
    : IEventHandler<UserDeleted>
{
    /// <summary>The subjects whose erasure fails.</summary>
    public static HashSet<string> FailFor { get; } = new(StringComparer.Ordinal);

    public Task HandleAsync(UserDeleted eventModel, CancellationToken ct)
    {
        bool fails;
        lock (FailFor)
        {
            fails = FailFor.Contains(eventModel.Subject);
        }

        return fails
            ? throw new InvalidOperationException("The Resumes module is unavailable.")
            : new UserDeletedHandler(scopes, logger).HandleAsync(eventModel, ct);
    }
}
