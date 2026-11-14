using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Resumes.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using AuthDbConst = Cvitlo.Auth.Infrastructure.Database.DbConstants;
using ResumesDbConst = Cvitlo.Resumes.Infrastructure.Database.DbConstants;

namespace Cvitlo.Migrations.IntegrationTests;

internal static class TestDbContexts
{
    internal static ResumesDbContext Resumes(string connectionString)
    {
        return new ResumesDbContext(new DbContextOptionsBuilder<ResumesDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                ResumesDbConst.MigrationsHistoryTableName, ResumesDbConst.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
    }

    internal static AuthDbContext Auth(string connectionString)
    {
        return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                AuthDbConst.MigrationsHistoryTableName, AuthDbConst.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
    }

    internal static async Task ShouldHaveEveryMigrationAppliedAsync(this DbContext context, CancellationToken ct)
    {
        var applied = await context.Database.GetAppliedMigrationsAsync(ct);

        applied.ShouldNotBeEmpty();
        applied.ShouldBe(context.Database.GetMigrations());
    }
}
