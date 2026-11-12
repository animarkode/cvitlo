using FastEndpoints;

namespace Cvitlo.Resumes.Application.Groups;

public sealed class LanguagesSubGroup : SubGroup<ResumesGroup>
{
    public LanguagesSubGroup()
    {
        Configure(
            "{ResumeId}/languages",
            ep =>
            {
                ep.Tags("Languages");
            });
    }
}