using System.Threading.RateLimiting;
using Cvitlo.Api.RateLimiting.Options;
using Cvitlo.Api.RateLimiting.Policies;
using Cvitlo.Auth.Application.Authentication;
using Cvitlo.Common.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Cvitlo.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCvitloRateLimiting(this IServiceCollection services)
    {
        services.ConfigureOptions<RateLimitingOptionsSetup>();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = RateLimitRejectionHandler.HandleAsync;

            // Options are resolved per request (a dictionary lookup) rather than bound once at startup,
            // so IOptions<> consumers and the limiters can never drift apart.
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var options = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

                return RateLimitPartitions.ByUser(context) is { } userKey
                    ? RateLimitPartitions.SlidingWindow(userKey, options.GlobalAuthenticated)
                    : RateLimitPartitions.SlidingWindow(RateLimitPartitions.ByIp(context), options.GlobalAnonymous);
            });
            limiter.AddPolicy<string, AuthSessionPolicy>(RateLimitPolicies.AuthSession);
            limiter.AddPolicy<string, AuthExternalPolicy>(RateLimitPolicies.AuthExternal);
            limiter.AddPolicy<string, AuthenticatedDefaultPolicy>(RateLimitPolicies.AuthenticatedDefault);
            limiter.AddPolicy<string, ExpensiveResourcePolicy>(RateLimitPolicies.ExpensiveResource);
        });

        return services;
    }

    // The OIDC handler answers the callback inside UseAuthentication, so the general limiter, which
    // runs after authentication to know the user, never sees it. Call this before UseAuthentication.
    public static WebApplication UseCvitloCallbackRateLimiting(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        if (!options.Enabled)
        {
            return app;
        }

        var limiter = new RateLimiterOptions
        {
            RejectionStatusCode = StatusCodes.Status429TooManyRequests,
            OnRejected = (context, ct) => RateLimitRejectionHandler.HandleAsync(context, RateLimitPolicies.AuthExternal, ct),
            GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartitions.SlidingWindow(
                    RateLimitPartitions.ByIp(context),
                    context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value.AuthExternal))
        };

        app.UseWhen(
            context => context.Request.Path.Equals(AuthSchemes.CallbackPath, StringComparison.OrdinalIgnoreCase),
            branch => branch.UseRateLimiter(limiter));

        return app;
    }

    public static WebApplication UseCvitloRateLimiting(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        if (!options.Enabled)
        {
            return app;
        }

        app.UseRateLimiter();

        return app;
    }
}
