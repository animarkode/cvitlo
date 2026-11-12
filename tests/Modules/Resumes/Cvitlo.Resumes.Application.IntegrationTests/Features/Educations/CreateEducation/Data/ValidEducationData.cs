using System.Collections;
using Cvitlo.Resumes.Application.Features.Educations.CreateEducation;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using static Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.ResumesTestConstants;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Educations.CreateEducation.Data;

public class ValidEducationData : IEnumerable<TheoryDataRow<CreateEducationRequest>>
{
    private const int Count = 3;

    public IEnumerator<TheoryDataRow<CreateEducationRequest>> GetEnumerator()
    {
        foreach (var r in new CreateEducationRequestFaker(Guid.Empty, EducationSeed).Generate(Count))
        {
            yield return new TheoryDataRow<CreateEducationRequest>(r);
        }

        yield return new TheoryDataRow<CreateEducationRequest>(
            new CreateEducationRequest(Guid.Empty, null, null, null, null, null, null));

        yield return new TheoryDataRow<CreateEducationRequest>(
            new CreateEducationRequest(Guid.Empty, string.Empty, string.Empty, null, null, null, null));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
