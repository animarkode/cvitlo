using FastEndpoints;

namespace Cvitlo.Resumes.Application.Groups;

public sealed class PersonsSubGroup : SubGroup<ResumesGroup>
{
    public PersonsSubGroup()
    {
        Configure(
            "{ResumeId}/persons",
            ep =>
            {
                ep.Tags("Persons");
            });
    }
}