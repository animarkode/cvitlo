using FastEndpoints;

namespace Cvitlo.Resumes.Application.Groups;

public sealed class ProjectsSubGroup : SubGroup<ResumesGroup>
{
    public ProjectsSubGroup()
    {
        Configure("{ResumeId}/projects", ep => ep.Tags("Projects"));
    }
}
