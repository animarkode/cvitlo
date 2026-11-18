using System.Text.Json.Serialization;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateSummary;

public record UpdateSummaryRequest(Guid ResumeId, [property: JsonRequired] string? Summary);
