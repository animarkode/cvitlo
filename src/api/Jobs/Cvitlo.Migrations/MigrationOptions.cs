using System.Diagnostics.CodeAnalysis;

namespace Cvitlo.Migrations;

public sealed class MigrationOptions
{
    public const string SectionName = "Migrations";

    // Schemas owned by something other than EF Core (Keycloak's Liquibase) that must exist before their
    // owner starts; the worker only ensures each exists and never migrates its contents.
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "Bound from configuration as a string array and only read by the worker.")]
    public string[] ExternalSchemas { get; init; } = [];
}
