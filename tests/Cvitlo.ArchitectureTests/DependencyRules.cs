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
        return Violations(
            sources.SelectMany(source => source.GetTypeDependencies().Select(dependency => (Source: source, Target: dependency))),
            type => type.FullName,
            isForbidden,
            (source, target) => known.Any(k => k.Covers(source, target)));
    }

    public static IReadOnlyList<string> Violations(
        Func<string, string, bool> isForbidden,
        IReadOnlyList<KnownProjectReference>? known = null)
    {
        known ??= [];
        return Violations(
            ProjectGraph.Edges.Select(edge => (Source: edge.From, Target: edge.To)),
            name => name,
            isForbidden,
            (source, target) => known.Any(k => k.Covers(new ProjectEdge(source, target))));
    }

    private static List<string> Violations<TNode>(
        IEnumerable<(TNode Source, TNode Target)> edges,
        Func<TNode, string> describe,
        Func<TNode, TNode, bool> isForbidden,
        Func<TNode, TNode, bool> isKnown) =>
        edges
            .Where(edge => isForbidden(edge.Source, edge.Target) && !isKnown(edge.Source, edge.Target))
            .Select(edge => $"{describe(edge.Source)} -> {describe(edge.Target)}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
