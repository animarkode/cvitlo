using System.Diagnostics.CodeAnalysis;
using Bogus;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

public class CreateLinkRequestFaker : Faker<CreateLinkRequest>
{
    [SuppressMessage("Usage", "MA0056:Do not call overridable members in constructor",
        Justification = "Bogus configures a Faker through its virtual fluent methods; the faker is not subclassed further.")]
    public CreateLinkRequestFaker(Guid resumeId, int seed = ResumesTestConstants.LinkSeed)
    {
        UseSeed(seed);
        CustomInstantiator(f => new CreateLinkRequest(
            resumeId,
            f.PickRandom("GitHub", "LinkedIn", "Portfolio", "Twitter", "Blog", "GitLab"),
            f.Internet.Url()));
    }
}
