using System.Reflection;
using Microsoft.Extensions.Options;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;

namespace Cvitlo.Migrations.IntegrationTests;

// Runs the job through its real entry point, the way its container does, so the wiring in
// Program.cs (connection string, history tables, option validation) is what gets tested.
[Collection(MigrationExitCodeGroup.Name)]
public class MigrationsProgramTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17.6").Build();

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Program_EmptyDatabase_ShouldApplyEveryMigrationAndExitWithZero()
    {
        var ct = TestContext.Current.CancellationToken;
        var connectionString = _postgres.GetConnectionString();

        var exitCode = await RunProgramAsync($"--ConnectionStrings:cvitlodb={connectionString}");

        exitCode.ShouldBe(0);
        await using var resumes = TestDbContexts.Resumes(connectionString);
        await resumes.ShouldHaveEveryMigrationAppliedAsync(ct);
        await using var auth = TestDbContexts.Auth(connectionString);
        await auth.ShouldHaveEveryMigrationAppliedAsync(ct);
    }

    [Fact]
    public async Task Program_ExternalSchemaDeclared_ShouldCreateItAndExitWithZero()
    {
        var ct = TestContext.Current.CancellationToken;
        var connectionString = _postgres.GetConnectionString();

        var exitCode = await RunProgramAsync(
            $"--ConnectionStrings:cvitlodb={connectionString}",
            "--Migrations:ExternalSchemas:0=keycloak");

        exitCode.ShouldBe(0);
        (await SchemaExistsAsync(connectionString, "keycloak", ct)).ShouldBeTrue();
    }

    [Fact]
    public async Task Program_ConnectionStringMissing_ShouldFailBeforeTheHostStarts()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => RunProgramAsync());

        exception.Message.ShouldContain("cvitlodb");
    }

    [Fact]
    public async Task Program_BlankExternalSchema_ShouldFailValidationAndLeaveTheDatabaseUntouched()
    {
        var ct = TestContext.Current.CancellationToken;
        var connectionString = _postgres.GetConnectionString();

        await Should.ThrowAsync<OptionsValidationException>(() => RunProgramAsync(
            $"--ConnectionStrings:cvitlodb={connectionString}",
            "--Migrations:ExternalSchemas:0= "));

        (await SchemaExistsAsync(connectionString, Resumes.Infrastructure.Database.DbConstants.SchemaName, ct))
            .ShouldBeFalse("a rejected configuration must not migrate anything");
    }

    private static async Task<int> RunProgramAsync(params string[] args)
    {
        Environment.ExitCode = 0;

        try
        {
            var entryPoint = typeof(MigrationWorker).Assembly.EntryPoint
                ?? throw new InvalidOperationException("Cvitlo.Migrations has no entry point.");

            // The entry point blocks until the host stops, so it gets its own thread.
            await Task.Run(
                () => entryPoint.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, [args], culture: null),
                TestContext.Current.CancellationToken);

            return Environment.ExitCode;
        }
        finally
        {
            Environment.ExitCode = 0;
        }
    }

    private static async Task<bool> SchemaExistsAsync(string connectionString, string schema, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema)";
        command.Parameters.AddWithValue("schema", schema);

        return (bool)(await command.ExecuteScalarAsync(ct))!;
    }
}
