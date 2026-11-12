using System.Collections;
using Cvitlo.Resumes.Application.Features.Courses.CreateCourse;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Courses.CreateCourse.Data;

public class ValidCourseData : IEnumerable<TheoryDataRow<CreateCourseRequest>>
{
    private const int Count = 3;

    public IEnumerator<TheoryDataRow<CreateCourseRequest>> GetEnumerator()
    {
        foreach (var r in new CreateCourseRequestFaker(Guid.Empty).Generate(Count))
        {
            yield return new TheoryDataRow<CreateCourseRequest>(r);
        }

        yield return new TheoryDataRow<CreateCourseRequest>(
            new CreateCourseRequest(Guid.Empty, null, null, null, null, null));

        yield return new TheoryDataRow<CreateCourseRequest>(
            new CreateCourseRequest(Guid.Empty, string.Empty, string.Empty, null, null, null));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}