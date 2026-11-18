using System.Text.Json.Serialization;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateTitle;

public record UpdateTitleRequest(Guid ResumeId, [property: JsonRequired] string? Title);
