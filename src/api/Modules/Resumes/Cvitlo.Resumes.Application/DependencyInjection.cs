using Cvitlo.Common;
using Cvitlo.Resumes.Application.Authorization;
using Cvitlo.Resumes.Application.Caching;
using Cvitlo.Resumes.Application.Features.Documents.Shared;
using Cvitlo.Resumes.Application.Options;
using Cvitlo.Resumes.Infrastructure.Database;
using Cvitlo.Resumes.PublicApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cvitlo.Resumes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddResumesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IResumesModuleApi, ResumesModuleApi>();

        services.AddScoped<ResumesCacheInvalidationInterceptor>();
        services.AddScoped<ResumePdfCache>();

        services.ConfigureOptions<ResumeLimitsOptionsSetup>();
        services.AddScoped<IAuthorizationHandler, MaxResumeCreationHandler>();
        services.AddScoped<IAuthorizationHandler, ResumeOwnershipHandler>();

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                "MaxResumeCreationPolicy",
                p =>
                    p.Requirements.Add(new MaxResumeCreationRequirement()))
            .AddPolicy(
                "ResumeOwnerPolicy",
                p =>
                    p.Requirements.Add(new ResumeOwnershipRequirement()));

        var connectionString = configuration.GetRequiredConnectionString("faberdb");

        services.AddDbContext<ResumesDbContext>((sp, o) => o
            .UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(
                        DbConstants.MigrationsHistoryTableName,
                        DbConstants.SchemaName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<ResumesCacheInvalidationInterceptor>())
        );

        return services;
    }
}