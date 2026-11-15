using System.Net.Http.Headers;
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

    /// <summary>
    /// Returns a new client signed in as <paramref name="user"/> with a bearer token. The caller disposes it;
    /// the shared <c>Client</c> is never changed.
    /// </summary>
    public async Task<HttpClient> CreateBearerClientAsync(TestUser user, CancellationToken ct = default)
    {
        var accessToken = await GetAccessTokenAsync(user, ct);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    /// <summary>Signs <paramref name="user"/> in with Keycloak's password grant.</summary>
    public async Task<string> GetAccessTokenAsync(TestUser user, CancellationToken ct = default)
    {
        var tokens = await Keycloak.GetTokensAsync(user.Email, user.Password, ct);

        return tokens["access_token"]!.GetValue<string>();
    }

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
        var ct = TestContext.Current.CancellationToken;

        _network = new NetworkBuilder().WithName($"cvitlo-test-network-{Guid.NewGuid()}").Build();
        await _network.CreateAsync(ct);

        _postgres = new PostgreSqlBuilder("postgres:17.6").WithNetwork(_network).Build();
        _redis = UsesRedis ? new RedisBuilder("redis:7.4").WithNetwork(_network).Build() : null;

        // Keycloak needs only the network, so it starts alongside the data stores.
        var keycloak = CvitloKeycloak.StartAsync(_network, WebOrigin.TrimEnd('/'), BackchannelLogoutUrl, ct);
        var startTasks = new List<Task> { keycloak, _postgres.StartAsync(ct) };
        if (_redis is not null)
        {
            startTasks.Add(_redis.StartAsync(ct));
        }

        await Task.WhenAll(startTasks);
        _keycloak = await keycloak;

        await OnKeycloakStartedAsync(ct);

        // Before the API starts: Data Protection reads its key table on first use.
        await MigrateAsync<AuthDbContext>(
            options => new AuthDbContext(options),
            AuthDbConstants.MigrationsHistoryTableName,
            AuthDbConstants.SchemaName,
            ct);
        await MigrateAsync<ResumesDbContext>(
            options => new ResumesDbContext(options),
            ResumesDbConstants.MigrationsHistoryTableName,
            ResumesDbConstants.SchemaName,
            ct);

        await SeedUsersAsync(ct);
    }

    private async Task MigrateAsync<TContext>(
        Func<DbContextOptions<TContext>, TContext> create,
        string historyTable,
        string schema,
        CancellationToken ct)
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(PostgresConnectionString, o => o.MigrationsHistoryTable(historyTable, schema))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = create(options);
        await context.Database.MigrateAsync(ct);
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
