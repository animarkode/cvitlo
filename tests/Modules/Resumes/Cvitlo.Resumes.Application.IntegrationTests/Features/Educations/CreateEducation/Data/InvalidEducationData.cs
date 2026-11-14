using System.Collections;
using Cvitlo.Resumes.Application.Features.Educations.CreateEducation;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Educations.CreateEducation.Data;

public class InvalidEducationData : IEnumerable<TheoryDataRow<CreateEducationRequest, string>>
{
    public IEnumerator<TheoryDataRow<CreateEducationRequest, string>> GetEnumerator()
    {
        var baseRequest = new CreateEducationRequestFaker(Guid.NewGuid()).Generate();

        yield return new TheoryDataRow<CreateEducationRequest, string>(
            baseRequest with { School = new string('a', 101) },
            "school");

        yield return new TheoryDataRow<CreateEducationRequest, string>(
            baseRequest with { Degree = new string('b', 101) },
            "degree");

        yield return new TheoryDataRow<CreateEducationRequest, string>(
            baseRequest with { City = new string('b', 101) },
            "city");

        yield return new TheoryDataRow<CreateEducationRequest, string>(
            baseRequest with { Description = new string('A', 1001) },
            "description");

        yield return new TheoryDataRow<CreateEducationRequest, string>(
            baseRequest with { StartDate = new DateOnly(2024, 12, 31), EndDate = new DateOnly(2024, 1, 1) },
            "endDate");

        yield return new TheoryDataRow<CreateEducationRequest, string>(
            baseRequest with { StartDate = new DateOnly(2099, 1, 1), EndDate = null },
            "startDate");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
