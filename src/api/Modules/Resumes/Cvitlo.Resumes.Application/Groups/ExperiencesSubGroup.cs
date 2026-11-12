using FastEndpoints;

namespace Cvitlo.Resumes.Application.Groups;

public sealed class ExperiencesSubGroup : SubGroup<ResumesGroup>
{
    public ExperiencesSubGroup()
    {
        Configure(
            "{ResumeId}/experiences",
            ep =>
            {
                ep.Tags("Experiences");
            });
    }
}
