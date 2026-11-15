namespace Cvitlo.ArchitectureTests;

public readonly record struct ModuleAssembly(string Module, Layer Layer)
{
    private const string Prefix = "Cvitlo.";

    // Accepts any "Cvitlo.{Module}.{Layer}" name, so assembly and project names parse alike.
    public static bool TryParse(string? name, out ModuleAssembly result)
    {
        result = default;
        if (name is null || !name.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = name[Prefix.Length..].Split('.');
        if (parts.Length != 2 || !Enum.TryParse(parts[1], out Layer layer) || !Enum.IsDefined(layer))
        {
            return false;
        }

        result = new ModuleAssembly(parts[0], layer);
        return true;
    }
}
