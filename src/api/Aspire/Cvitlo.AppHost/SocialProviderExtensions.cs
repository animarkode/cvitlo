using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cvitlo.AppHost;

// A provider is enabled only when Parameters:{alias}-client-id is set in user-secrets. Its parameters
// are declared only then, so the dashboard never lists an unconfigured one as missing;
// the warning is what tells the developer the provider is off.
internal static class SocialProviderExtensions
{
    public static IResourceBuilder<ContainerResource> WithSocialProvider(
        this IResourceBuilder<ContainerResource> keycloakConfig,
        IDistributedApplicationBuilder builder,
        string alias)
    {
        var prefix = alias.ToUpperInvariant();
        var clientIdParameter = $"{alias}-client-id";
        var configured = !string.IsNullOrWhiteSpace(builder.Configuration[$"Parameters:{clientIdParameter}"]);

        keycloakConfig.WithEnvironment($"{prefix}_ENABLED", configured ? "true" : "false");

        if (!configured)
        {
            return keycloakConfig.WithDisabledProviderWarning(alias, clientIdParameter);
        }

        var clientId = builder.AddParameter(clientIdParameter)
            .WithDescription($"OAuth client ID of the {alias} app that Keycloak signs users in with.");
        var clientSecret = builder.AddParameter($"{alias}-client-secret", secret: true)
            .WithDescription($"OAuth client secret of the {alias} app.");

        return keycloakConfig
            .WithEnvironment($"{prefix}_CLIENT_ID", clientId)
            .WithEnvironment($"{prefix}_CLIENT_SECRET", clientSecret);
    }

    // Hangs off this resource's own start event: a global event would wait on health gates.
    private static IResourceBuilder<ContainerResource> WithDisabledProviderWarning(
        this IResourceBuilder<ContainerResource> keycloakConfig,
        string alias,
        string clientIdParameter)
    {
        keycloakConfig.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            keycloakConfig.Resource,
            (@event, _) =>
            {
                var hostLogger = @event.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(SocialProviderExtensions));
                var resourceLogger = @event.Services.GetRequiredService<ResourceLoggerService>()
                    .GetLogger(@event.Resource);

                foreach (var logger in new[] { hostLogger, resourceLogger })
                {
                    logger.LogWarning(
                        "{Provider} sign-in is disabled: set the AppHost user-secrets Parameters:{ClientIdParameter} and Parameters:{ClientSecretParameter} to enable it (see CONTRIBUTING.md)",
                        alias,
                        clientIdParameter,
                        $"{alias}-client-secret");
                }

                return Task.CompletedTask;
            });

        return keycloakConfig;
    }
}
