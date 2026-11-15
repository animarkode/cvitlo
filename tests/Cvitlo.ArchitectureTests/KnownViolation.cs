using ArchUnitNET.Domain;

namespace Cvitlo.ArchitectureTests;

internal sealed record KnownViolation(string Source, string Target)
{
    public bool Covers(IType source, IType target) =>
        Matches(source.FullName, Source) && Matches(target.FullName, Target);

    // Razor and the compiler emit nested helper types ("+") that belong to the exact type.
    private static bool Matches(string fullName, string exact) =>
        string.Equals(fullName, exact, StringComparison.Ordinal)
        || fullName.StartsWith(exact + "+", StringComparison.Ordinal);
}
