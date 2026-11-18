using System.Text.Json.Serialization;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateHobbies;

public record UpdateHobbiesRequest(Guid ResumeId, [property: JsonRequired] string? Hobbies);
