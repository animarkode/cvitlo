using Shouldly;

namespace Cvitlo.ArchitectureTests;

// A ProjectReference without type usage never reaches IL, so the type rules cannot see it.
public class ProjectReferenceTests
{
    private static bool IsOuterLayer(ModuleAssembly module) => module.Layer is Layer.Application or Layer.Infrastructure;

    private static List<string> Violations(Func<string, string, bool> isForbidden, IReadOnlyList<KnownProjectReference>? known = null)
    {
        known ??= [];
        return ProjectGraph.Edges
            .Where(edge => isForbidden(edge.From, edge.To) && !known.Any(k => k.Covers(edge)))
            .Select(edge => $"{edge.From} -> {edge.To}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    [Fact]
    public void Modules_ShouldReferenceOtherModulesOnlyThroughPublicApi()
    {
        var violations = Violations(
            (from, to) => ModuleAssembly.TryParse(from, out var source)
                && ModuleAssembly.TryParse(to, out var target)
                && !string.Equals(source.Module, target.Module, StringComparison.Ordinal)
                && target.Layer != Layer.PublicApi,
            KnownViolations.ProjectReferences);

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Domain_ShouldNotReferenceApplicationOrInfrastructure()
    {
        var violations = Violations((from, to) =>
            ModuleAssembly.TryParse(from, out var source) && source.Layer == Layer.Domain
            && ModuleAssembly.TryParse(to, out var target) && IsOuterLayer(target));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void PublicApi_ShouldNotReferenceApplicationOrInfrastructure()
    {
        var violations = Violations((from, to) =>
            ModuleAssembly.TryParse(from, out var source) && source.Layer == Layer.PublicApi
            && ModuleAssembly.TryParse(to, out var target) && IsOuterLayer(target));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Common_ShouldNotReferenceAnyModule()
    {
        var violations = Violations((from, to) =>
            string.Equals(from, CvitloArchitecture.CommonAssembly, StringComparison.Ordinal)
            && ModuleAssembly.TryParse(to, out _));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Modules_ShouldNotReferenceHost()
    {
        var violations = Violations((from, to) =>
            ModuleAssembly.TryParse(from, out _)
            && string.Equals(to, CvitloArchitecture.HostAssembly, StringComparison.Ordinal));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Architecture_ShouldLoadEveryProductionProject()
    {
        var expected = ProjectGraph.Projects.Where(ProjectGraph.IsProductionProject).ToList();
        var loaded = CvitloArchitecture.Architecture.Assemblies
            .Select(assembly => assembly.Name)
            .Where(ProjectGraph.IsProductionProject)
            .ToHashSet(StringComparer.Ordinal);

        expected.ShouldNotBeEmpty();
        var missing = expected.Where(name => !loaded.Contains(name)).ToList();
        var unknown = loaded.Where(name => !expected.Contains(name)).Order(StringComparer.Ordinal).ToList();

        missing.ShouldBeEmpty("Add these projects to Cvitlo.ArchitectureTests.csproj: " + string.Join(", ", missing));
        unknown.ShouldBeEmpty("Loaded assemblies without a project under src/api: " + string.Join(", ", unknown));
    }
}
