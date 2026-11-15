using System.Diagnostics.CodeAnalysis;

namespace Cvitlo.Resumes.Application.Features.Documents.Shared;

[SuppressMessage(
    "Performance",
    "CA1819:Properties should not return arrays",
    Justification = "Immutable rendered payload that is cached and serialized as a byte array.")]
public record ResumePdf(byte[] Content, string FileName);
