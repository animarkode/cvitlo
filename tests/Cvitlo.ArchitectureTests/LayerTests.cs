using Shouldly;

namespace Cvitlo.ArchitectureTests;

public class LayerTests
{
    private static bool IsOuterLayer(Layer layer) => layer is Layer.Application or Layer.Infrastructure;

    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrInfrastructure()
    {
        var violations = DependencyRules.Violations(
            CvitloArchitecture.TypesOf(m => m.Layer == Layer.Domain),
            (_, target) => CvitloArchitecture.IsModuleType(target, out var to) && IsOuterLayer(to.Layer));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void PublicApi_ShouldNotDependOnApplicationOrInfrastructure()
    {
        var violations = DependencyRules.Violations(
            CvitloArchitecture.TypesOf(m => m.Layer == Layer.PublicApi),
            (_, target) => CvitloArchitecture.IsModuleType(target, out var to) && IsOuterLayer(to.Layer));

        violations.ShouldBeEmpty();
    }
}
