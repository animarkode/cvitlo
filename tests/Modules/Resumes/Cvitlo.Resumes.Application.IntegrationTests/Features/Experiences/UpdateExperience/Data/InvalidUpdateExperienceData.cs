using System.Collections;
using Cvitlo.Resumes.Application.Features.Experiences.UpdateExperience;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Experiences.UpdateExperience.Data;

public class InvalidUpdateExperienceData : IEnumerable<TheoryDataRow<UpdateExperienceRequest, string>>
{
    public IEnumerator<TheoryDataRow<UpdateExperienceRequest, string>> GetEnumerator()
    {
        var source = new CreateExperienceRequestFaker(Guid.Empty).Generate();
        var baseRequest = new UpdateExperienceRequest(Guid.NewGuid(), Guid.Empty, source.JobTitle, source.Employer, source.StartDate, source.EndDate, source.City, source.Description);

        yield return new TheoryDataRow<UpdateExperienceRequest, string>(
            baseRequest with { JobTitle = new string('a', 101) },
            "jobTitle");

        yield return new TheoryDataRow<UpdateExperienceRequest, string>(
            baseRequest with { Employer = new string('b', 101) },
            "employer");

        yield return new TheoryDataRow<UpdateExperienceRequest, string>(
            baseRequest with { City = new string('b', 101) },
            "city");

        yield return new TheoryDataRow<UpdateExperienceRequest, string>(
            baseRequest with { Description = new string('A', 1001) },
            "description");

        yield return new TheoryDataRow<UpdateExperienceRequest, string>(
            baseRequest with { StartDate = new DateOnly(2024, 12, 31), EndDate = new DateOnly(2024, 1, 1) },
            "endDate");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}