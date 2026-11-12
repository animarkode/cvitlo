using FastEndpoints;

namespace Cvitlo.Resumes.Application.Groups;

public sealed class EducationsSubGroup : SubGroup<ResumesGroup>
{
    public EducationsSubGroup()
    {
        Configure(
            "{ResumeId}/educations",
            ep =>
            {
                ep.Tags("Educations");
            });
    }
}