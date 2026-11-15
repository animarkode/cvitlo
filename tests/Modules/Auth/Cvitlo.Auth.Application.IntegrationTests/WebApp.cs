using Cvitlo.Auth.Application.Sessions;
using Cvitlo.Auth.Application.IntegrationTests.Support;
using Cvitlo.Auth.PublicApi.Events;
using Cvitlo.Common.Testing.Containers;
using Cvitlo.Common.Testing.Data;
using Cvitlo.Common.Testing.Hosting;
using Cvitlo.Common.Testing.Http;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Cvitlo.Auth.Application.IntegrationTests;

public class WebApp : CvitloWebApp
{
    /// <summary>
    /// The origin the "browser" uses for the API. It is HTTPS so that the client keeps and sends the
    /// <c>Secure</c> <c>__Host-</c> session cookie, exactly as a browser would.
    /// </summary>
    public static readonly Uri AppOrigin = new("https://localhost");

    public BackchannelLogoutRelay Relay { get; } = new();

    /// <summary>The clock the API reads; tests move it forward to make access tokens look expired.</summary>
    public RealignableClock Clock { get; } = new(DateTimeOffset.UtcNow);

    /// <summary>Lets a test make Keycloak's token endpoint misbehave for the API's back-channel calls.</summary>
    public TokenEndpointInterceptor KeycloakBackChannel { get; } = new();

    public TokenEndpointInterceptor KeycloakAccountBackChannel { get; } = new();

    /// <summary>The same for the Admin API client, which deletes accounts.</summary>
    public TokenEndpointInterceptor KeycloakAdminBackChannel { get; } = new();

    /// <summary>A second realm in the same Keycloak that plays Google or GitHub.</summary>
    public static readonly StandInIdentityProvider StandIn = new(
        Alias: "standin",
        Realm: "standin",
        ClientId: "cvitlo-broker",
        ClientSecret: "broker-secret",
        Username: "provider.user",
        Password: "provider-password-1",
        Email: "provider.user@standin.test",
        FirstName: "Stan",
        LastName: "Din");

    /// <summary>A client that behaves like a browser tab on the web app: cookies on, redirects off.</summary>
    public HttpClient CreateBrowserClient()
    {
        var client = CreateClient(new ClientOptions
        {
            BaseAddress = AppOrigin,
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-CSRF", "1");

        return client;
    }

    /// <summary>Signs a user in through Keycloak's login page and returns the signed-in browser client.</summary>
    public async Task<(HttpClient Client, HttpResponseMessage Callback)> SignInAsync(
        TestUser user,
        bool rememberMe = false,
        string returnUrl = "/",
        CancellationToken ct = default)
    {
        var client = CreateBrowserClient();
        using var driver = new KeycloakLoginDriver(client);
        var callback = await driver.SignInAsync(user.Email, user.Password, rememberMe, returnUrl, ct);

        return (client, callback);
    }

    protected override string WebOrigin => AppOrigin.ToString();

    protected override string BackchannelLogoutUrl => Relay.ContainerUrl;

    protected override async Task OnKeycloakStartedAsync(CancellationToken ct) =>
        await Keycloak.Container.AddStandInIdentityProviderAsync(
            CvitloKeycloak.AdminUsername, CvitloKeycloak.AdminPassword, CvitloKeycloak.Realm, StandIn, ct);

    protected override ValueTask SetupAsync()
    {
        Relay.Start(() => CreateClient(new ClientOptions { BaseAddress = AppOrigin }));
        return ValueTask.CompletedTask;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(Clock);
        services.RegisterTestEventHandler<UserDeleted, FailingUserDeletedHandler>();
        services.AddHttpClient<KeycloakTokenClient>().AddHttpMessageHandler(KeycloakBackChannel.CreateHandler);
        services.AddHttpClient<KeycloakAccountClient>().AddHttpMessageHandler(KeycloakAccountBackChannel.CreateHandler);
        services.AddHttpClient<KeycloakAdminClient>().AddHttpMessageHandler(KeycloakAdminBackChannel.CreateHandler);
    }

    protected override async ValueTask TearDownAsync()
    {
        await Relay.DisposeAsync();
        await base.TearDownAsync();
    }
}
