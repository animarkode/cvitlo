using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;

namespace Cvitlo.ArchitectureTests;

internal static class DependencyRules
{
    public static IReadOnlyList<string> Violations(
        IEnumerable<IType> sources,
        Func<IType, IType, bool> isForbidden,
        IReadOnlyList<KnownViolation>? known = null)
    {
        known ??= [];
        return sources
            .SelectMany(source => source.GetTypeDependencies()
                .Select(dependency => (Source: source, Target: dependency)))
            .Where(pair => isForbidden(pair.Source, pair.Target) && !known.Any(k => k.Covers(pair.Source, pair.Target)))
            .Select(pair => $"{pair.Source.FullName} -> {pair.Target.FullName}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
