namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateTitle;

public record UpdateTitleRequest(Guid ResumeId, string? Title);
