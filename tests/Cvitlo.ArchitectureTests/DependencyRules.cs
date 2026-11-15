using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;

namespace Cvitlo.ArchitectureTests;

internal static class DependencyRules
{
    public static IReadOnlyList<string> Violations(
        IEnumerable<IType> sources,
        Func<IType, IType, bool> isForbidden) =>
        Violations(
            sources.SelectMany(source => source.GetTypeDependencies().Select(dependency => (Source: source, Target: dependency))),
            type => type.FullName,
            isForbidden);

    public static IReadOnlyList<string> Violations(Func<string, string, bool> isForbidden) =>
        Violations(
            ProjectGraph.Edges.Select(edge => (Source: edge.From, Target: edge.To)),
            name => name,
            isForbidden);

    private static List<string> Violations<TNode>(
        IEnumerable<(TNode Source, TNode Target)> edges,
        Func<TNode, string> describe,
        Func<TNode, TNode, bool> isForbidden) =>
        edges
            .Where(edge => isForbidden(edge.Source, edge.Target))
            .Select(edge => $"{describe(edge.Source)} -> {describe(edge.Target)}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
