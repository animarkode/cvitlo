using Cvitlo.Auth.Application.Groups;
using Cvitlo.Common.RateLimiting;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Auth.Application.Features.BackchannelLogout;

public class BackchannelLogoutEndpoint : Endpoint<BackchannelLogoutRequest, Results<Ok, BadRequest>>
{
    public override void Configure()
    {
        Post("backchannel-logout");
        Group<AuthGroup>();
        Version(1);
        AllowAnonymous();
        AllowFormData(urlEncoded: true);
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthSession));
    }

    public override async Task<Results<Ok, BadRequest>> ExecuteAsync(BackchannelLogoutRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.LogoutToken))
        {
            return TypedResults.BadRequest();
        }

        var result = await new BackchannelLogoutCommand(request.LogoutToken).ExecuteAsync(ct);

        return result.IsError ? TypedResults.BadRequest() : TypedResults.Ok();
    }
}
