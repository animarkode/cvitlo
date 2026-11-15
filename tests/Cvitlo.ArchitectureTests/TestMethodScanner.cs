using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cvitlo.ArchitectureTests;

// Syntax trees, not reflection, so this project need not reference every test project.
internal static class TestMethodScanner
{
    private static readonly Lazy<IReadOnlyList<TestMethod>> LazyMethods = new(Scan);

    public static IReadOnlyList<TestMethod> Methods => LazyMethods.Value;

    private static List<TestMethod> Scan()
    {
        var root = FindRepositoryRoot();
        return Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .SelectMany(file => MethodsIn(root, file))
            .ToList();
    }

    private static IEnumerable<TestMethod> MethodsIn(string root, string file)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
        var relative = Path.GetRelativePath(root, file).Replace('\\', '/');

        return tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Select(method => (Method: method, Attributes: TestAttributes(method).ToList()))
            .Where(x => x.Attributes.Count > 0)
            .Select(x => new TestMethod(
                relative,
                (x.Method.Parent as TypeDeclarationSyntax)?.Identifier.Text ?? "<unknown>",
                x.Method.Identifier.Text,
                x.Attributes.Any(a => a.ArgumentList?.Arguments.Any(arg => string.Equals(arg.NameEquals?.Name.Identifier.Text, "DisplayName", StringComparison.Ordinal)) == true)));
    }

    private static IEnumerable<AttributeSyntax> TestAttributes(MethodDeclarationSyntax method) =>
        method.AttributeLists.SelectMany(list => list.Attributes).Where(IsFactOrTheory);

    private static bool IsFactOrTheory(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        return name is "Fact" or "FactAttribute" or "Xunit.Fact" or "Xunit.FactAttribute"
            or "Theory" or "TheoryAttribute" or "Xunit.Theory" or "Xunit.TheoryAttribute";
    }

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
