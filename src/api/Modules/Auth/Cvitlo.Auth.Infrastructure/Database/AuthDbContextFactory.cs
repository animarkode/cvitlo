using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cvitlo.Auth.Infrastructure.Database;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{environment}.json", true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("cvitlodb")
            ?? throw new InvalidOperationException("Connection string 'cvitlodb' not found");

        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();

        optionsBuilder
            .UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    DbConstants.MigrationsHistoryTableName,
                    DbConstants.SchemaName))
            .UseSnakeCaseNamingConvention();

        return new AuthDbContext(optionsBuilder.Options);
    }
}
