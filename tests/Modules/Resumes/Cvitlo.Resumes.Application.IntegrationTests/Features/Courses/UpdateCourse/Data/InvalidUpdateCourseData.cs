using System.Collections;
using Cvitlo.Resumes.Application.Features.Courses.UpdateCourse;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.UpdateCourse.Data;

public class InvalidUpdateCourseData : IEnumerable<TheoryDataRow<UpdateCourseRequest, string>>
{
    public IEnumerator<TheoryDataRow<UpdateCourseRequest, string>> GetEnumerator()
    {
        var source = new CreateCourseRequestFaker(Guid.Empty).Generate();
        var baseRequest = new UpdateCourseRequest(Guid.NewGuid(), Guid.Empty, source.School, source.Name, source.StartDate, source.EndDate, source.Description);

        yield return new TheoryDataRow<UpdateCourseRequest, string>(
            baseRequest with { Name = new string('a', 101) },
            "name");

        yield return new TheoryDataRow<UpdateCourseRequest, string>(
            baseRequest with { School = new string('b', 101) },
            "school");

        yield return new TheoryDataRow<UpdateCourseRequest, string>(
            baseRequest with { Description = new string('A', 1001) },
            "description");

        yield return new TheoryDataRow<UpdateCourseRequest, string>(
            baseRequest with { StartDate = new DateOnly(2024, 12, 31), EndDate = new DateOnly(2024, 1, 1) },
            "endDate");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}