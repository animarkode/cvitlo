using Shouldly;

namespace Cvitlo.ArchitectureTests;

public class CvitloArchitectureTests
{
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

    // Type rules key on assembly names and graph rules on project file names; they agree only if these match.
    [Fact]
    public void ProductionProjects_ShouldUseFileNameAsAssemblyName()
    {
        var drifted = ProjectGraph.AssemblyNameOverrides()
            .Where(o => !string.Equals(o.Project, o.AssemblyName, StringComparison.Ordinal))
            .Select(o => $"{o.Project} -> {o.AssemblyName}")
            .ToList();

        drifted.ShouldBeEmpty();
    }
}
