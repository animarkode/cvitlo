using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ReflectionAssembly = System.Reflection.Assembly;

namespace Cvitlo.ArchitectureTests;

public static class CvitloArchitecture
{
    public const string CommonAssembly = "Cvitlo.Common";
    public const string HostAssembly = "Cvitlo.Api";

    private static readonly Lazy<Architecture> Lazy = new(Load);

    public static Architecture Architecture => Lazy.Value;

    public static bool IsModuleType(IType type, out ModuleAssembly module) =>
        ModuleAssembly.TryParse(type.Assembly.Name, out module);

    public static bool IsModuleType(IType type) => IsModuleType(type, out _);

    public static bool IsAssembly(IType type, string assemblyName) =>
        string.Equals(type.Assembly.Name, assemblyName, StringComparison.Ordinal);

    public static IEnumerable<IType> TypesOf(Func<ModuleAssembly, bool> predicate) =>
        Architecture.Types.Where(t => IsModuleType(t, out var module) && predicate(module));

    public static IEnumerable<string> Modules() =>
        Architecture.Types
            .Select(t => IsModuleType(t, out var module) ? module.Module : null)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

    // Production assemblies are copied next to the test assembly through its ProjectReferences.
    private static Architecture Load()
    {
        var directory = Path.GetDirectoryName(typeof(CvitloArchitecture).Assembly.Location);
        var assemblies = Directory.EnumerateFiles(directory ?? AppContext.BaseDirectory, "Cvitlo.*.dll")
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .Where(name => name is CommonAssembly or HostAssembly || ModuleAssembly.TryParse(name, out _))
            .Select(name => ReflectionAssembly.Load(name))
            .ToArray();

        return new ArchLoader().LoadAssemblies(assemblies).Build();
    }
}
