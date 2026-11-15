using Shouldly;

namespace Cvitlo.ArchitectureTests;

// A ProjectReference without type usage never reaches IL, so the type rules cannot see it.
public class ProjectReferenceTests
{
    [Fact]
    public void Modules_ShouldReferenceOtherModulesOnlyThroughPublicApi()
    {
        var violations = DependencyRules.Violations(
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
        var violations = DependencyRules.Violations((from, to) =>
            ModuleAssembly.TryParse(from, out var source) && source.Layer == Layer.Domain
            && ModuleAssembly.TryParse(to, out var target) && target.IsOuterLayer);

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void PublicApi_ShouldNotReferenceApplicationOrInfrastructure()
    {
        var violations = DependencyRules.Violations((from, to) =>
            ModuleAssembly.TryParse(from, out var source) && source.Layer == Layer.PublicApi
            && ModuleAssembly.TryParse(to, out var target) && target.IsOuterLayer);

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Common_ShouldNotReferenceAnyModule()
    {
        var violations = DependencyRules.Violations((from, to) =>
            string.Equals(from, CvitloArchitecture.CommonAssembly, StringComparison.Ordinal)
            && ModuleAssembly.TryParse(to, out _));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Modules_ShouldNotReferenceHost()
    {
        var violations = DependencyRules.Violations((from, to) =>
            ModuleAssembly.TryParse(from, out _)
            && string.Equals(to, CvitloArchitecture.HostAssembly, StringComparison.Ordinal));

        violations.ShouldBeEmpty();
    }
}
