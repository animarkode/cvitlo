using FastEndpoints;

namespace Cvitlo.Resumes.Application.Groups;

public sealed class CoursesSubGroup : SubGroup<ResumesGroup>
{
    public CoursesSubGroup()
    {
        Configure(
            "{ResumeId}/courses",
            ep =>
            {
                ep.Tags("Courses");
            });
    }
}