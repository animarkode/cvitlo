using FastEndpoints;

namespace Cvitlo.Resumes.Application.Groups;

public sealed class ResumesGroup : Group
{
    public ResumesGroup()
    {
        Configure(
            "resumes",
            ep =>
            {
                ep.Tags("Resumes");
            });
    }
}