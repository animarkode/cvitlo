using System.Collections;
using Cvitlo.Resumes.Application.Features.Educations.UpdateEducation;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Educations.UpdateEducation.Data;

public class InvalidUpdateEducationData : IEnumerable<TheoryDataRow<UpdateEducationRequest, string>>
{
    public IEnumerator<TheoryDataRow<UpdateEducationRequest, string>> GetEnumerator()
    {
        var source = new CreateEducationRequestFaker(Guid.Empty).Generate();
        var baseRequest = new UpdateEducationRequest(Guid.NewGuid(), Guid.Empty, source.School, source.Degree, source.StartDate, source.EndDate, source.City, source.Description);

        yield return new TheoryDataRow<UpdateEducationRequest, string>(
            baseRequest with { School = new string('a', 101) },
            "school");

        yield return new TheoryDataRow<UpdateEducationRequest, string>(
            baseRequest with { Degree = new string('b', 101) },
            "degree");

        yield return new TheoryDataRow<UpdateEducationRequest, string>(
            baseRequest with { City = new string('b', 101) },
            "city");

        yield return new TheoryDataRow<UpdateEducationRequest, string>(
            baseRequest with { Description = new string('A', 1001) },
            "description");

        yield return new TheoryDataRow<UpdateEducationRequest, string>(
            baseRequest with { StartDate = new DateOnly(2024, 12, 31), EndDate = new DateOnly(2024, 1, 1) },
            "endDate");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
