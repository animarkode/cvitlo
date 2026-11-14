using System.Collections;
using Cvitlo.Resumes.Application.Features.Courses.CreateCourse;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.CreateCourse.Data;

public class InvalidCourseData : IEnumerable<TheoryDataRow<CreateCourseRequest, string>>
{
    public IEnumerator<TheoryDataRow<CreateCourseRequest, string>> GetEnumerator()
    {
        var baseRequest = new CreateCourseRequestFaker(Guid.NewGuid()).Generate();

        yield return new TheoryDataRow<CreateCourseRequest, string>(
            baseRequest with { Name = new string('a', 101) },
            "name");

        yield return new TheoryDataRow<CreateCourseRequest, string>(
            baseRequest with { School = new string('b', 101) },
            "school");

        yield return new TheoryDataRow<CreateCourseRequest, string>(
            baseRequest with { Description = new string('A', 1001) },
            "description");

        yield return new TheoryDataRow<CreateCourseRequest, string>(
            baseRequest with { StartDate = new DateOnly(2024, 12, 31), EndDate = new DateOnly(2024, 1, 1) },
            "endDate");

        yield return new TheoryDataRow<CreateCourseRequest, string>(
            baseRequest with { StartDate = new DateOnly(2099, 1, 1), EndDate = null },
            "startDate");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}