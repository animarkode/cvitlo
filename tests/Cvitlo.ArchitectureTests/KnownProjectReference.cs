namespace Cvitlo.ArchitectureTests;

internal sealed record KnownProjectReference(string From, string To)
{
    public bool Covers(ProjectEdge edge) =>
        string.Equals(edge.From, From, StringComparison.Ordinal) && string.Equals(edge.To, To, StringComparison.Ordinal);
}
