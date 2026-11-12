using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Auth.Application.Sessions;

/// <summary>Runs <see cref="PendingErasureSweeper"/> once an hour.</summary>
public sealed class PendingErasureSweepService(
    IServiceScopeFactory scopes,
    ILogger<PendingErasureSweepService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var resolved = await scope.ServiceProvider.GetRequiredService<PendingErasureSweeper>().SweepAsync(stoppingToken);
                logger.LogInformation("Finished {Count} pending account erasure(s)", resolved);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Sweeping pending account erasures failed; retrying in an hour");
            }
        }
    }
}
