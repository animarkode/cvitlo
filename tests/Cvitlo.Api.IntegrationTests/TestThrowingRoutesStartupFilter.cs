using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Api.IntegrationTests;

/// <summary>
/// Adds routes that throw, so tests can observe how the API's exception handlers answer. The
/// middleware is added after the app's own pipeline: it runs only when no endpoint matched, and
/// inside <c>UseExceptionHandler</c>, which is where a failing endpoint would throw.
/// </summary>
public class TestThrowingRoutesStartupFilter : IStartupFilter
{
    public const string PathPrefix = "/__test/throw";

    public const string SecretMessage = "secret-internal-detail";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            next(app);

            app.Use((context, nextMiddleware) =>
            {
                if (!context.Request.Path.StartsWithSegments(PathPrefix, StringComparison.Ordinal, out var kind))
                {
                    return nextMiddleware(context);
                }

                throw kind.Value switch
                {
                    "/concurrency" => new DbUpdateConcurrencyException(SecretMessage),
                    "/db-update" => new DbUpdateException(SecretMessage),
                    "/validation" => new ValidationException(
                    [
                        new ValidationFailure("Title", "Title is required."),
                        new ValidationFailure("Title", "Title is too long."),
                        new ValidationFailure("Summary", "Summary is too long.")
                    ]),
                    "/cancelled" => new OperationCanceledException(SecretMessage),
                    _ => new InvalidOperationException(SecretMessage)
                };
            });
        };
    }
}
