using Shouldly;

namespace Cvitlo.ArchitectureTests;

public class ModuleBoundaryTests
{
    [Fact]
    public void Modules_ShouldReachOtherModulesOnlyThroughPublicApi()
    {
        var violations = DependencyRules.Violations(
            CvitloArchitecture.TypesOf(_ => true),
            (source, target) => CvitloArchitecture.IsModuleType(source, out var from)
                && CvitloArchitecture.IsModuleType(target, out var to)
                && !string.Equals(from.Module, to.Module, StringComparison.Ordinal)
                && to.Layer != Layer.PublicApi);

        violations.ShouldBeEmpty();
    }
}
