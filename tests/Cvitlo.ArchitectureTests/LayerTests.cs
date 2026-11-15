using Shouldly;

namespace Cvitlo.ArchitectureTests;

public class LayerTests
{
    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrInfrastructure()
    {
        var violations = DependencyRules.Violations(
            CvitloArchitecture.TypesOf(m => m.Layer == Layer.Domain),
            (_, target) => CvitloArchitecture.IsModuleType(target, out var to) && to.IsOuterLayer);

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void PublicApi_ShouldNotDependOnApplicationOrInfrastructure()
    {
        var violations = DependencyRules.Violations(
            CvitloArchitecture.TypesOf(m => m.Layer == Layer.PublicApi),
            (_, target) => CvitloArchitecture.IsModuleType(target, out var to) && to.IsOuterLayer);

        violations.ShouldBeEmpty();
    }
}
