using System.Collections;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using static Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.ResumesTestConstants;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.CreateExperience.Data;

public class ValidExperienceData : IEnumerable<TheoryDataRow<CreateExperienceRequest>>
{
    private const int Count = 3;

    public IEnumerator<TheoryDataRow<CreateExperienceRequest>> GetEnumerator()
    {
        foreach (var r in new CreateExperienceRequestFaker(Guid.Empty, ExperienceSeed).Generate(Count))
        {
            yield return new TheoryDataRow<CreateExperienceRequest>(r);
        }

        yield return new TheoryDataRow<CreateExperienceRequest>(
            new CreateExperienceRequest(Guid.Empty, null, null, null, null, null, null));

        yield return new TheoryDataRow<CreateExperienceRequest>(
            new CreateExperienceRequest(Guid.Empty, string.Empty, string.Empty, null, null, null, null));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
