using System.Globalization;
using Cvitlo.Migrations;
using Cvitlo.Auth.Infrastructure.Database;
using Cvitlo.Resumes.Infrastructure.Database;
using Cvitlo.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Serilog;
using AuthDbConst = Cvitlo.Auth.Infrastructure.Database.DbConstants;
using ResumesDbConst = Cvitlo.Resumes.Infrastructure.Database.DbConstants;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSerilog((_, config) => config.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture), writeToProviders: true);

var connectionString = builder.Configuration.GetConnectionString("cvitlodb")
    ?? throw new InvalidOperationException("Connection string 'cvitlodb' is required.");

builder.Services.AddDbContext<ResumesDbContext>(o => o
    .UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable(
            ResumesDbConst.MigrationsHistoryTableName,
            ResumesDbConst.SchemaName))
    .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<AuthDbContext>(o => o
    .UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable(
            AuthDbConst.MigrationsHistoryTableName,
            AuthDbConst.SchemaName))
    .UseSnakeCaseNamingConvention());

builder.Services.AddOptions<MigrationOptions>()
    .BindConfiguration(MigrationOptions.SectionName)
    .Validate(
        o => o.ExternalSchemas.All(schema => !string.IsNullOrWhiteSpace(schema)),
        $"{MigrationOptions.SectionName}:{nameof(MigrationOptions.ExternalSchemas)} must not contain blank schema names.")
    .ValidateOnStart();

builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();
await host.RunAsync();
