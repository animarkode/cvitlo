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
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"));

    private static List<string> LoadProjects() =>
        ProjectFiles().Select(NameOf).Order(StringComparer.Ordinal).ToList();

    private static List<ProjectEdge> LoadEdges() =>
        ProjectFiles()
            .SelectMany(file => XDocument.Load(file).Descendants("ProjectReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .OfType<string>()
                .Select(include => new ProjectEdge(NameOf(file), NameOf(include))))
            .ToList();

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
