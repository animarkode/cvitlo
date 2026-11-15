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
    // The origin the "browser" uses for the API. It is HTTPS so that the client keeps and sends the
    // Secure __Host- session cookie, exactly as a browser would.
    public static readonly Uri AppOrigin = new("https://localhost");

    public BackchannelLogoutRelay Relay { get; } = new();

    public RealignableClock Clock { get; } = new(DateTimeOffset.UtcNow);

    public TokenEndpointInterceptor KeycloakBackChannel { get; } = new();

    public TokenEndpointInterceptor KeycloakAccountBackChannel { get; } = new();

    public TokenEndpointInterceptor KeycloakAdminBackChannel { get; } = new();

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
