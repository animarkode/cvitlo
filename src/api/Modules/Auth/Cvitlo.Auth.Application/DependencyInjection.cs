using Cvitlo.Auth.Application.Authentication;
using Cvitlo.Auth.Application.Features.Account;
using Cvitlo.Auth.Application.Features.BackchannelLogout;
using Cvitlo.Auth.Application.Options;
using Cvitlo.Auth.Application.Sessions;
using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cvitlo.Auth.Application;

public static class DependencyInjection
{
    /// <summary>The application name all API instances share, so each can read the others' cookies and sessions.</summary>
    private const string DataProtectionApplicationName = "Cvitlo";

    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureOptions<KeycloakOptionsSetup>();
        services.AddOptions<KeycloakOptions>()
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.ClientId),
                "Keycloak:ClientId must be configured.")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.ClientSecret),
                "Keycloak:ClientSecret must be configured.")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.AdminClientId) && !string.IsNullOrWhiteSpace(o.AdminClientSecret),
                "Keycloak:AdminClientId and Keycloak:AdminClientSecret must be configured.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);

        var connectionString = configuration.GetRequiredConnectionString("cvitlodb");

        services.AddDbContext<AuthDbContext>(o => o
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable(DbConstants.MigrationsHistoryTableName, DbConstants.SchemaName))
            .UseSnakeCaseNamingConvention());

        services.AddDataProtection()
            .SetApplicationName(DataProtectionApplicationName)
            .PersistKeysToDbContext<AuthDbContext>();

        services.AddSingleton<PostgresTicketStore>();
        services.AddScoped<SessionRefresher>();
        services.AddScoped<ExpiredSessionSweeper>();
        services.AddHostedService<ExpiredSessionSweepService>();
        services.AddScoped<PendingErasureSweeper>();
        services.AddHostedService<PendingErasureSweepService>();
        services.AddScoped<LogoutTokenValidator>();
        services.AddScoped<AccountSignInMethodsLock>();
        // Short: a refresh holds the session's row lock while it waits.
        services.AddHttpClient<KeycloakTokenClient>(client => client.Timeout = TimeSpan.FromSeconds(5));
        services.AddHttpClient<KeycloakAccountClient>(client => client.Timeout = TimeSpan.FromSeconds(5));
        services.AddHttpClient<KeycloakAdminClient>(client => client.Timeout = TimeSpan.FromSeconds(5));

        services.AddCvitloAuthentication();

        return services;
    }

    /// <summary>Requires the CSRF header on cookie-authenticated API calls. Call before authentication.</summary>
    public static IApplicationBuilder UseCvitloCsrfProtection(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CsrfHeaderMiddleware>();
    }
}
