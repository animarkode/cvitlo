using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Common.Testing.Containers;
using Cvitlo.Common.Testing.Data;
using Cvitlo.Resumes.Infrastructure.Database;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using FastEndpoints.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using AuthDbConstants = Cvitlo.Auth.Infrastructure.Database.DbConstants;
using ResumesDbConstants = Cvitlo.Resumes.Infrastructure.Database.DbConstants;

namespace Cvitlo.Common.Testing.Hosting;

/// <summary>
/// The API on a network with real Postgres, Keycloak and, on request, Redis. Derived fixtures
/// override the virtual members; <see cref="ConfigureApp"/> and <see cref="TearDownAsync"/> must call base.
/// </summary>
public abstract class CvitloWebApp : AppFixture<Program>
{
    private CvitloKeycloak? _keycloak;
    private INetwork? _network;
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;

    public CvitloKeycloak Keycloak => _keycloak ?? throw new InvalidOperationException("Keycloak not initialized");

    public List<TestUser> Users { get; } = RegisteredUsersData.Generate();

    protected string PostgresConnectionString => Postgres.GetConnectionString();

    protected virtual bool UsesRedis => false;

    protected virtual string WebOrigin => "https://localhost";

    protected virtual string BackchannelLogoutUrl => "http://localhost/backchannel-logout";

    private PostgreSqlContainer Postgres =>
        _postgres ?? throw new InvalidOperationException("Postgres container not initialized");

    private RedisContainer Redis =>
        _redis ?? throw new InvalidOperationException("Redis container not initialized");

    public Task StopPostgresAsync() => Postgres.StopAsync();

    /// <summary>Runs after Keycloak starts and before the schemas are migrated and users seeded.</summary>
    protected virtual Task OnKeycloakStartedAsync(CancellationToken ct) => Task.CompletedTask;

    protected virtual async Task SeedUsersAsync(CancellationToken ct)
    {
        foreach (var user in Users)
        {
            await Keycloak.Admin.CreateUserAsync(user, ct: ct);
        }
    }

    protected override async ValueTask PreSetupAsync()
    {
        _network = new NetworkBuilder().WithName($"cvitlo-test-network-{Guid.NewGuid()}").Build();
        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder("postgres:17.6").WithNetwork(_network).Build();
        _redis = UsesRedis ? new RedisBuilder("redis:7.4").WithNetwork(_network).Build() : null;

        var startTasks = new List<Task> { _postgres.StartAsync() };
        if (_redis is not null)
        {
            startTasks.Add(_redis.StartAsync());
        }

        await Task.WhenAll(startTasks);
        _keycloak = await CvitloKeycloak.StartAsync(_network, WebOrigin.TrimEnd('/'), BackchannelLogoutUrl);

        await OnKeycloakStartedAsync(CancellationToken.None);

        // Before the API starts: Data Protection reads its key table on first use.
        await using (var auth = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
                         .UseNpgsql(PostgresConnectionString, o => o.MigrationsHistoryTable(
                             AuthDbConstants.MigrationsHistoryTableName, AuthDbConstants.SchemaName))
                         .UseSnakeCaseNamingConvention()
                         .Options))
        {
            await auth.Database.MigrateAsync();
        }

        await using (var resumes = new ResumesDbContext(new DbContextOptionsBuilder<ResumesDbContext>()
                         .UseNpgsql(PostgresConnectionString, o => o.MigrationsHistoryTable(
                             ResumesDbConstants.MigrationsHistoryTableName, ResumesDbConstants.SchemaName))
                         .UseSnakeCaseNamingConvention()
                         .Options))
        {
            await resumes.Database.MigrateAsync();
        }

        await SeedUsersAsync(CancellationToken.None);
    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:cvitlodb", PostgresConnectionString);

        if (UsesRedis)
        {
            builder.UseSetting("ConnectionStrings:redis", Redis.GetConnectionString());
        }

        builder.UseSetting("Keycloak:BaseUrl", Keycloak.BaseAddress);
        builder.UseSetting("Keycloak:Realm", CvitloKeycloak.Realm);
        builder.UseSetting("Keycloak:Issuer", Keycloak.Issuer);
        builder.UseSetting("Keycloak:Audience", CvitloKeycloak.ClientId);
        builder.UseSetting("Keycloak:ClientId", CvitloKeycloak.ClientId);
        builder.UseSetting("Keycloak:ClientSecret", CvitloKeycloak.ClientSecret);
        builder.UseSetting("Keycloak:AdminClientSecret", CvitloKeycloak.AdminClientSecret);
        builder.UseSetting("Keycloak:RequireHttpsMetadata", "false");
        builder.UseSetting("RateLimiting:Enabled", "false");
    }

    protected override async ValueTask TearDownAsync()
    {
        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }

        if (_keycloak is not null)
        {
            await _keycloak.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DisposeAsync();
        }
    }
}
