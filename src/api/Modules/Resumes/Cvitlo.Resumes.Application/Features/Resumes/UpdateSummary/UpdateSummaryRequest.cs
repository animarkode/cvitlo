namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateSummary;

public record UpdateSummaryRequest(Guid ResumeId, string? Summary);