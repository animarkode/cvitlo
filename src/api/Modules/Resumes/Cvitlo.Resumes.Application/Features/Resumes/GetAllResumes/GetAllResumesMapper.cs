using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Domain.Entities;

namespace Cvitlo.Resumes.Application.Features.Resumes.GetAllResumes;

public static class GetAllResumesMapper
{
    public static GetAllResumesResponse MapToResponse(this IReadOnlyList<Resume> resumes)
    {
        return new GetAllResumesResponse(resumes.Select(r => r.ToResponse()).ToList());
    }
}
