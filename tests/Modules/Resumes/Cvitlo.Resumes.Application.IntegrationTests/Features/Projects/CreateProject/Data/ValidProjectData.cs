using System.Collections;
using Cvitlo.Resumes.Application.Features.Projects.CreateProject;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Projects.CreateProject.Data;

public class ValidProjectData : IEnumerable<TheoryDataRow<CreateProjectRequest>>
{
    private const int Count = 3;

    public IEnumerator<TheoryDataRow<CreateProjectRequest>> GetEnumerator()
    {
        foreach (var r in new CreateProjectRequestFaker(Guid.Empty).Generate(Count))
        {
            yield return new TheoryDataRow<CreateProjectRequest>(r);
        }

        yield return new TheoryDataRow<CreateProjectRequest>(
            new CreateProjectRequest(Guid.Empty, null, null, null, null, null, null));

        yield return new TheoryDataRow<CreateProjectRequest>(
            new CreateProjectRequest(Guid.Empty, string.Empty, string.Empty, null, null, null, null));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}