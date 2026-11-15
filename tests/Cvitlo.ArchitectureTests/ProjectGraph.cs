using System.Xml.Linq;

namespace Cvitlo.ArchitectureTests;

internal static class ProjectGraph
{
    private static readonly Lazy<IReadOnlyList<ProjectEdge>> LazyEdges = new(LoadEdges);
    private static readonly Lazy<IReadOnlyList<string>> LazyProjects = new(LoadProjects);

    public static IReadOnlyList<ProjectEdge> Edges => LazyEdges.Value;

    public static IReadOnlyList<string> Projects => LazyProjects.Value;

    public static bool IsProductionProject(string name) =>
        name is CvitloArchitecture.CommonAssembly or CvitloArchitecture.HostAssembly
        || ModuleAssembly.TryParse(name, out _);

    // Only "Cvitlo.*" projects count: stale local directories of the old name are not part of the repo.
    private static IEnumerable<string> ProjectFiles() =>
        Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), "src", "api"), "Cvitlo.*.csproj", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file));

    // Explicit AssemblyName values in a project or any Directory.Build.props above it.
    public static IReadOnlyList<(string Project, string AssemblyName)> AssemblyNameOverrides()
    {
        var root = FindRepositoryRoot();
        return ProjectFiles()
            .SelectMany(file => ScopeFiles(root, file)
                .SelectMany(scope => XDocument.Load(scope).Descendants()
                    .Where(e => string.Equals(e.Name.LocalName, "AssemblyName", StringComparison.Ordinal))
                    .Select(e => (Project: NameOf(file), AssemblyName: e.Value.Trim()))))
            .ToList();
    }

    private static IEnumerable<string> ScopeFiles(string root, string projectFile)
    {
        yield return projectFile;
        for (var dir = new DirectoryInfo(Path.GetDirectoryName(projectFile)!);
             dir is not null && dir.FullName.StartsWith(root, StringComparison.Ordinal);
             dir = dir.Parent)
        {
            var props = Path.Combine(dir.FullName, "Directory.Build.props");
            if (File.Exists(props))
            {
                yield return props;
            }
        }
    }

    private static List<string> LoadProjects() =>
        ProjectFiles().Select(NameOf).Order(StringComparer.Ordinal).ToList();

    private static List<ProjectEdge> LoadEdges()
    {
        var projects = ProjectFiles().ToList();
        var fromProjects = projects.SelectMany(file => ReferencesIn(file).Select(to => new ProjectEdge(NameOf(file), to)));

        // Over-approximation: a reference in a props/targets file applies to every project at or below it.
        var fromProps = PropsFiles().SelectMany(props => projects
            .Where(file => IsBelow(Path.GetDirectoryName(props)!, file))
            .SelectMany(file => ReferencesIn(props).Select(to => new ProjectEdge(NameOf(file), to))));

        return fromProjects.Concat(fromProps).Distinct().ToList();
    }

    // Only the file name matters, so the include path needs no resolving against the props directory.
    private static IEnumerable<string> ReferencesIn(string file) =>
        XDocument.Load(file).Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
            .Select(e => e.Attribute("Include")?.Value)
            .OfType<string>()
            .Select(NameOf);

    private static bool IsBelow(string directory, string file) =>
        Path.GetFullPath(file).StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private static IEnumerable<string> PropsFiles()
    {
        var apiDirectory = Path.Combine(FindRepositoryRoot(), "src", "api");
        var inherited = new[] { FindRepositoryRoot(), Path.Combine(FindRepositoryRoot(), "src"), apiDirectory }
            .SelectMany(dir => new[] { "Directory.Build.props", "Directory.Build.targets" }.Select(name => Path.Combine(dir, name)))
            .Where(File.Exists);
        var local = new[] { "*.props", "*.targets" }
            .SelectMany(pattern => Directory.EnumerateFiles(apiDirectory, pattern, SearchOption.AllDirectories))
            .Where(file => !IsBuildOutput(file));

        return inherited.Concat(local).Distinct(StringComparer.Ordinal);
    }

    private static bool IsBuildOutput(string file) =>
        file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj");

    private static string NameOf(string path) => Path.GetFileNameWithoutExtension(path.Replace('\\', '/'));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cvitlo.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Cvitlo.sln was not found above " + AppContext.BaseDirectory);
    }
}
