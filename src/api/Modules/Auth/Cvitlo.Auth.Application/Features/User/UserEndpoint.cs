using Cvitlo.Auth.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Auth.Application.Features.User;

// Baseline-only for rate limiting: the SPA calls it on every load,
// and an anonymous call is the global-anonymous probe.
public class UserEndpoint : EndpointWithoutRequest<Ok<UserResponse>>
{
    public override void Configure()
    {
        Get("user");
        Group<AuthGroup>();
        Version(1);
    }

    public override Task<Ok<UserResponse>> ExecuteAsync(CancellationToken ct)
    {
        return Task.FromResult(TypedResults.Ok(User.MapToResponse()));
    }
}
