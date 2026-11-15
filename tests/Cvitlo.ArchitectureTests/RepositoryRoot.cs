namespace Cvitlo.ArchitectureTests;

internal static class RepositoryRoot
{
    private static readonly Lazy<string> LazyPath = new(Find);

    public static string Path => LazyPath.Value;

    private static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "Cvitlo.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Cvitlo.sln was not found above " + AppContext.BaseDirectory);
    }
}
