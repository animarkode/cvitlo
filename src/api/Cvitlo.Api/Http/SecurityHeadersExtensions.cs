namespace Cvitlo.Api.Http;

// The Development API reference pages (Scalar, Swagger) need scripts, so they keep the browser defaults.
public static class SecurityHeadersExtensions
{
    private static readonly string[] DevelopmentDocumentationPaths = ["/scalar", "/swagger", "/openapi"];

    public static IApplicationBuilder UseCvitloSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            app.UseHsts();
        }

        return app.Use((context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            var isDocumentation = environment.IsDevelopment()
                                  && DevelopmentDocumentationPaths.Any(path => context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase));
            if (!isDocumentation)
            {
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            }

            return next(context);
        });
    }
}
