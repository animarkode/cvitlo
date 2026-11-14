using Microsoft.AspNetCore.Diagnostics;

namespace Cvitlo.Api.ExceptionHandlers;

public class OperationCanceledExceptionHandler(ILogger<OperationCanceledExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not OperationCanceledException)
        {
            return ValueTask.FromResult(false);
        }

        // The middleware answers a client abort itself, so what arrives here was cancelled on the server side.
        logger.LogWarning(exception, "Request was cancelled on the server side at {Path}", httpContext.Request.Path.Value);

        httpContext.Response.StatusCode = 499; // Client Closed Request

        return ValueTask.FromResult(true);
    }
}