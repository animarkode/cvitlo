using Shouldly;

namespace Cvitlo.ArchitectureTests;

public class SharedKernelTests
{
    [Fact]
    public void Common_ShouldNotDependOnAnyModule()
    {
        var violations = DependencyRules.Violations(
            CvitloArchitecture.Architecture.Types.Where(t => CvitloArchitecture.IsAssembly(t, CvitloArchitecture.CommonAssembly)),
            (_, target) => CvitloArchitecture.IsModuleType(target));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Modules_ShouldNotDependOnHost()
    {
        var violations = DependencyRules.Violations(
            CvitloArchitecture.TypesOf(_ => true),
            (_, target) => CvitloArchitecture.IsAssembly(target, CvitloArchitecture.HostAssembly));

        violations.ShouldBeEmpty();
    }
}
