using System.Collections;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.CreateExperience.Data;

public class InvalidExperienceData : IEnumerable<TheoryDataRow<CreateExperienceRequest, string>>
{
    public IEnumerator<TheoryDataRow<CreateExperienceRequest, string>> GetEnumerator()
    {
        var baseRequest = new CreateExperienceRequestFaker(Guid.NewGuid()).Generate();

        yield return new TheoryDataRow<CreateExperienceRequest, string>(
            baseRequest with { JobTitle = new string('a', 101) },
            "jobTitle");

        yield return new TheoryDataRow<CreateExperienceRequest, string>(
            baseRequest with { Employer = new string('b', 101) },
            "employer");

        yield return new TheoryDataRow<CreateExperienceRequest, string>(
            baseRequest with { City = new string('b', 101) },
            "city");

        yield return new TheoryDataRow<CreateExperienceRequest, string>(
            baseRequest with { Description = new string('A', 1001) },
            "description");

        yield return new TheoryDataRow<CreateExperienceRequest, string>(
            baseRequest with { StartDate = new DateOnly(2024, 12, 31), EndDate = new DateOnly(2024, 1, 1) },
            "endDate");

        yield return new TheoryDataRow<CreateExperienceRequest, string>(
            baseRequest with { StartDate = new DateOnly(2099, 1, 1), EndDate = null },
            "startDate");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}