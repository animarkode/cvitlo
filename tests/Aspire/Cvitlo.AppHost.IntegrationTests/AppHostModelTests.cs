using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Cvitlo.AppHost.IntegrationTests;

public class AppHostModelTests
{
    private static readonly string[] TestConfiguration =
    [
        "--Parameters:postgres-username=test-postgres-user",
        "--Parameters:postgres-password=test-postgres-password",
        "--Parameters:keycloak-admin=test-keycloak-admin",
        "--Parameters:keycloak-password=test-keycloak-password",
        "--Parameters:cvitlo-api-client-secret=test-cvitlo-api-client-secret",
        "--Parameters:cvitlo-admin-client-secret=test-cvitlo-admin-client-secret",
        // The builder also reads the AppHost's user-secrets; blank the social providers so a
        // developer's real Google/GitHub credentials cannot enable them. Tests that need one set it after.
        "--Parameters:google-client-id=",
        "--Parameters:google-client-secret=",
        "--Parameters:github-client-id=",
        "--Parameters:github-client-secret="
    ];

    [Fact]
    public async Task DependencyGraph_ShouldGateCleanStackStartup()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);

        var cvitloDb = builder.Resources.Single(resource => string.Equals(resource.Name, "cvitlodb", StringComparison.Ordinal));
        var migrations = builder.Resources.Single(resource => string.Equals(resource.Name, "migrations", StringComparison.Ordinal));
        var keycloak = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak", StringComparison.Ordinal));
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));
        var keycloakPasskeys = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-passkeys", StringComparison.Ordinal));
        var api = builder.Resources.Single(resource => string.Equals(resource.Name, "cvitloapi", StringComparison.Ordinal));

        migrations.ShouldWaitFor(cvitloDb, WaitType.WaitUntilHealthy);
        keycloak.ShouldWaitFor(migrations, WaitType.WaitForCompletion);
        keycloakConfig.ShouldWaitFor(keycloak, WaitType.WaitUntilHealthy);
        keycloakPasskeys.ShouldWaitFor(keycloakConfig, WaitType.WaitForCompletion);
        api.ShouldWaitFor(migrations, WaitType.WaitForCompletion);
        api.ShouldWaitFor(keycloakPasskeys, WaitType.WaitForCompletion);
    }

    [Fact]
    public async Task Keycloak_ShouldRunTheStockImageWithoutRealmImport()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var keycloak = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak", StringComparison.Ordinal));

        var image = keycloak.Annotations.OfType<ContainerImageAnnotation>().Single();
        image.Registry.ShouldBe("quay.io");
        image.Image.ShouldBe("keycloak/keycloak");
        image.Tag.ShouldBe("26.8.1");
        // Only the login theme is mounted (read-only); the realm is applied by keycloak-config, not imported.
        var mount = keycloak.Annotations.OfType<ContainerMountAnnotation>().Single();
        mount.Type.ShouldBe(ContainerMountType.BindMount);
        mount.IsReadOnly.ShouldBeTrue();
        mount.Target.ShouldBe("/opt/keycloak/themes/cvitlo");
        Path.GetFullPath(mount.Source!).ShouldBe(Path.GetFullPath(Path.Combine(
            builder.AppHostDirectory, "..", "..", "..", "..", "infra", "keycloak", "themes", "cvitlo")));

        var configuration = await ExecutionConfigurationBuilder
            .Create(keycloak)
            .WithArgumentsConfig()
            .WithEnvironmentVariablesConfig()
            .BuildAsync(
                new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
                NullLogger.Instance,
                TestContext.Current.CancellationToken);

        configuration.Exception.ShouldBeNull();
        configuration.Arguments.Select(argument => argument.Value).ShouldBe(["start-dev"]);

        var environment = configuration.EnvironmentVariables.ToDictionary(variable => variable.Key, variable => variable.Value, StringComparer.Ordinal);
        environment["KC_DB"].ShouldBe("postgres");
        environment["KC_DB_SCHEMA"].ShouldBe("keycloak");
        environment["CVITLO_APP_URL"].ShouldBe("http://localhost:4200");
        // Keycloak must use the database the migration service created its schema in. Asserted by
        // database name rather than the full JDBC URL, which the public-snapshot secret scan flags
        // and has reviewed once, in Program.cs.
        var cvitloDb = builder.Resources.OfType<PostgresDatabaseResource>().Single();
        environment["KC_DB_URL"].ShouldEndWith($"/{cvitloDb.DatabaseName}");
    }

    [Fact]
    public async Task KeycloakConfig_ShouldApplyTheRealmConfigurationFromTheRepository()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));

        var mount = keycloakConfig.Annotations.OfType<ContainerMountAnnotation>().Single();
        mount.Type.ShouldBe(ContainerMountType.BindMount);
        mount.IsReadOnly.ShouldBeTrue();
        mount.Target.ShouldBe("/config");
        var expectedSource = Path.GetFullPath(Path.Combine(
            builder.AppHostDirectory, "..", "..", "..", "..", "infra", "keycloak", "config"));
        Path.GetFullPath(mount.Source!).ShouldBe(expectedSource);

        var environment = await GetDeclaredEnvironmentAsync(keycloakConfig);
        environment["IMPORT_FILES_LOCATIONS"].ShouldBe("/config/*");
        environment["IMPORT_VARSUBSTITUTION_ENABLED"].ShouldBe("true");
        environment["CVITLO_APP_URL"].ShouldBe("http://localhost:4200");
        environment["CVITLO_BACKCHANNEL_LOGOUT_URL"]
            .ShouldBe("http://host.docker.internal:5020/api/v1/auth/backchannel-logout");
        environment["KC_SMTP_HOST"].ShouldBe("mailpit");
    }

    [Fact]
    public async Task KeycloakPasskeys_ShouldRunTheScriptWithCredentialsInTheEnvironmentOnly()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var keycloak = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak", StringComparison.Ordinal));
        var passkeys = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-passkeys", StringComparison.Ordinal));

        var keycloakImage = keycloak.Annotations.OfType<ContainerImageAnnotation>().Single();
        var image = passkeys.Annotations.OfType<ContainerImageAnnotation>().Single();
        (image.Registry, image.Image, image.Tag).ShouldBe((keycloakImage.Registry, keycloakImage.Image, keycloakImage.Tag));
        ((ContainerResource)passkeys).Entrypoint.ShouldBe("/bin/bash");

        var mount = passkeys.Annotations.OfType<ContainerMountAnnotation>().Single();
        mount.Type.ShouldBe(ContainerMountType.BindMount);
        mount.IsReadOnly.ShouldBeTrue();
        var expectedSource = Path.GetFullPath(Path.Combine(
            builder.AppHostDirectory, "..", "..", "..", "..", "infra", "keycloak", "passkeys.sh"));
        Path.GetFullPath(mount.Source!).ShouldBe(expectedSource);

        var configuration = await ExecutionConfigurationBuilder
            .Create(passkeys)
            .WithArgumentsConfig()
            .WithEnvironmentVariablesConfig()
            .BuildAsync(
                new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
                NullLogger.Instance,
                TestContext.Current.CancellationToken);

        configuration.Exception.ShouldBeNull();
        configuration.Arguments.Select(argument => argument.Value).ShouldBe([mount.Target]);
        var environment = configuration.EnvironmentVariables.ToDictionary(variable => variable.Key, variable => variable.Value, StringComparer.Ordinal);
        environment["KEYCLOAK_USER"].ShouldBe("test-keycloak-admin");
        environment["KEYCLOAK_PASSWORD"].ShouldBe("test-keycloak-password");
    }

    [Fact]
    public async Task CvitloApiClientSecret_ShouldReachKeycloakConfigAndTheApi()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));
        var api = builder.Resources.Single(resource => string.Equals(resource.Name, "cvitloapi", StringComparison.Ordinal));

        var secret = builder.Resources.OfType<ParameterResource>()
            .Single(resource => string.Equals(resource.Name, "cvitlo-api-client-secret", StringComparison.Ordinal));

        var configEnvironment = await GetDeclaredEnvironmentAsync(keycloakConfig);
        var apiEnvironment = await GetDeclaredEnvironmentAsync(api);

        secret.Secret.ShouldBeTrue();
        configEnvironment["CVITLO_API_CLIENT_SECRET"].ShouldBeSameAs(secret);
        apiEnvironment["Keycloak__ClientSecret"].ShouldBeSameAs(secret);
        apiEnvironment.Keys.ShouldNotContain("Keycloak__ClientId", StringComparer.Ordinal, "the client ID comes from the API's appsettings.json");
    }

    [Fact]
    public async Task CvitloAdminClientSecret_ShouldReachKeycloakConfigAndTheApi()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));
        var api = builder.Resources.Single(resource => string.Equals(resource.Name, "cvitloapi", StringComparison.Ordinal));
        var secret = Parameter(builder, "cvitlo-admin-client-secret");

        var configEnvironment = await GetDeclaredEnvironmentAsync(keycloakConfig);
        var apiEnvironment = await GetDeclaredEnvironmentAsync(api);

        configEnvironment["CVITLO_ADMIN_CLIENT_SECRET"].ShouldBeSameAs(secret);
        apiEnvironment["Keycloak__AdminClientSecret"].ShouldBeSameAs(secret);
    }

    [Theory]
    [InlineData("postgres-password")]
    [InlineData("keycloak-password")]
    [InlineData("cvitlo-api-client-secret")]
    [InlineData("cvitlo-admin-client-secret")]
    public async Task SecretParameter_ShouldBeMarkedSecret(string name)
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);

        Parameter(builder, name).Secret.ShouldBeTrue();
    }

    // A literal in the environment would show the password in the dashboard and in the manifest;
    // a parameter reference is resolved only when the container starts.
    [Fact]
    public async Task Passwords_ShouldReachContainersAsParameterReferences()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var keycloak = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak", StringComparison.Ordinal));
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));
        var keycloakPasskeys = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-passkeys", StringComparison.Ordinal));
        var postgresPassword = Parameter(builder, "postgres-password");
        var keycloakPassword = Parameter(builder, "keycloak-password");

        var keycloakEnvironment = await GetDeclaredEnvironmentAsync(keycloak);
        keycloakEnvironment["KC_DB_PASSWORD"].ShouldBeSameAs(postgresPassword);
        keycloakEnvironment["KC_BOOTSTRAP_ADMIN_PASSWORD"].ShouldBeSameAs(keycloakPassword);
        (await GetDeclaredEnvironmentAsync(keycloakConfig))["KEYCLOAK_PASSWORD"].ShouldBeSameAs(keycloakPassword);
        (await GetDeclaredEnvironmentAsync(keycloakPasskeys))["KEYCLOAK_PASSWORD"].ShouldBeSameAs(keycloakPassword);

        builder.Resources.OfType<PostgresServerResource>().Single().PasswordParameter.ShouldBeSameAs(postgresPassword);
    }

    [Fact]
    public async Task SocialProvider_WithCredentials_ShouldPassTheClientSecretAsASecretParameter()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            [
                .. TestConfiguration,
                "--Parameters:google-client-id=test-google-id",
                "--Parameters:google-client-secret=test-google-secret"
            ],
            TestContext.Current.CancellationToken);
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));
        var secret = Parameter(builder, "google-client-secret");

        var environment = await GetDeclaredEnvironmentAsync(keycloakConfig);

        secret.Secret.ShouldBeTrue();
        environment["GOOGLE_CLIENT_SECRET"].ShouldBeSameAs(secret);
    }

    // Aspire picks the data directory from the image tag when the volume is added, and Postgres 18
    // moved that directory. Pinning the tag after the volume would mount the wrong path, and an
    // unpinned tag would start a newer Postgres on a volume it cannot read.
    [Fact]
    public async Task Postgres_ShouldPinItsImageTagAndMountThePerCheckoutDataVolume()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var postgres = builder.Resources.OfType<PostgresServerResource>().Single();
        var checkoutPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "..", ".."));

        postgres.Annotations.OfType<ContainerImageAnnotation>().Single().Tag.ShouldBe("17.6");

        var mount = postgres.Annotations.OfType<ContainerMountAnnotation>().Single();
        mount.Type.ShouldBe(ContainerMountType.Volume);
        mount.Source.ShouldBe(PostgresVolumeName.FromCheckoutPath(checkoutPath));
        mount.Target.ShouldBe("/var/lib/postgresql/data");
        mount.IsReadOnly.ShouldBeFalse();
    }

    [Fact]
    public async Task SocialProviders_WithoutCredentials_ShouldBeDisabled()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));

        var environment = await GetDeclaredEnvironmentAsync(keycloakConfig);

        environment["GOOGLE_ENABLED"].ShouldBe("false");
        environment["GITHUB_ENABLED"].ShouldBe("false");
        environment.Keys.ShouldNotContain("GOOGLE_CLIENT_SECRET", StringComparer.Ordinal);
        environment.Keys.ShouldNotContain("GITHUB_CLIENT_SECRET", StringComparer.Ordinal);
    }

    [Fact]
    public async Task SocialProvider_WithCredentials_ShouldBeEnabled()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            [
                .. TestConfiguration,
                "--Parameters:google-client-id=test-google-id",
                "--Parameters:google-client-secret=test-google-secret"
            ],
            TestContext.Current.CancellationToken);
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));

        var environment = await GetDeclaredEnvironmentAsync(keycloakConfig);

        environment["GOOGLE_ENABLED"].ShouldBe("true");
        environment["GITHUB_ENABLED"].ShouldBe("false");
        environment.Keys.ShouldContain("GOOGLE_CLIENT_ID", StringComparer.Ordinal);
        environment.Keys.ShouldContain("GOOGLE_CLIENT_SECRET", StringComparer.Ordinal);
    }

    [Fact]
    public async Task SocialProviders_WithoutCredentials_ShouldWarnOnStart()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);

        var warnings = await CollectKeycloakConfigStartWarningsAsync(builder);

        warnings.ShouldContain(warning => warning.StartsWith("google sign-in is disabled"));
        warnings.ShouldContain(warning => warning.StartsWith("github sign-in is disabled"));
    }

    [Fact]
    public async Task SocialProvider_WithCredentials_ShouldNotWarnOnStart()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            [
                .. TestConfiguration,
                "--Parameters:google-client-id=test-google-id",
                "--Parameters:google-client-secret=test-google-secret"
            ],
            TestContext.Current.CancellationToken);

        var warnings = await CollectKeycloakConfigStartWarningsAsync(builder);

        warnings.ShouldNotContain(warning => warning.StartsWith("google sign-in is disabled"));
        warnings.ShouldContain(warning => warning.StartsWith("github sign-in is disabled"));
    }

    [Fact]
    public async Task KeycloakSchema_ShouldBeDeclaredToMigrations()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cvitlo_AppHost>(
            TestConfiguration,
            TestContext.Current.CancellationToken);
        var migrations = builder.Resources.Single(resource => string.Equals(resource.Name, "migrations", StringComparison.Ordinal));
        var keycloak = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak", StringComparison.Ordinal));

        var migrationsEnvironment = await GetDeclaredEnvironmentAsync(migrations);
        var keycloakEnvironment = await GetDeclaredEnvironmentAsync(keycloak);

        migrationsEnvironment["Migrations__ExternalSchemas__0"].ShouldBe(keycloakEnvironment["KC_DB_SCHEMA"]);
    }

    private static ParameterResource Parameter(IDistributedApplicationTestingBuilder builder, string name)
    {
        return builder.Resources.OfType<ParameterResource>()
            .Single(resource => string.Equals(resource.Name, name, StringComparison.Ordinal));
    }

    // Raises keycloak-config's start event without starting anything, and returns what the
    // AppHost logged at warning level.
    private static async Task<IReadOnlyCollection<string>> CollectKeycloakConfigStartWarningsAsync(
        IDistributedApplicationTestingBuilder builder)
    {
        var keycloakConfig = builder.Resources.Single(resource => string.Equals(resource.Name, "keycloak-config", StringComparison.Ordinal));
        using var logs = new CollectingLoggerProvider();
        await using var services = new ServiceCollection()
            .AddLogging(logging => logging.AddProvider(logs))
            .AddSingleton<ResourceLoggerService>()
            .BuildServiceProvider();

        await builder.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(keycloakConfig, services),
            TestContext.Current.CancellationToken);

        return logs.Warnings;
    }

    // The declared values, before Aspire resolves them. Resolving the migration service's
    // environment would wait for Postgres's endpoint, which is only allocated in a running app.
    private static async Task<IReadOnlyDictionary<string, object>> GetDeclaredEnvironmentAsync(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resource,
            cancellationToken: TestContext.Current.CancellationToken);

        foreach (var callback in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await callback.Callback(context);
        }

        return context.EnvironmentVariables;
    }
}
