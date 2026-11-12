using Cvitlo.Resumes.Application.Features.Resumes.Shared;

namespace Cvitlo.Resumes.Application.Features.Resumes.GetAllResumes;

public record GetAllResumesResponse(IReadOnlyList<ResumeResponse> Items);
