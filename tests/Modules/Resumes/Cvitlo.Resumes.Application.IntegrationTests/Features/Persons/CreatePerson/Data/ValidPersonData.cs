using System.Collections;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using static Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.ResumesTestConstants;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Persons.CreatePerson.Data;

public class ValidPersonData : IEnumerable<TheoryDataRow<CreatePersonRequest>>
{
    private const int Count = 3;

    public IEnumerator<TheoryDataRow<CreatePersonRequest>> GetEnumerator()
    {
        foreach (var r in new CreatePersonRequestFaker(Guid.Empty, PersonSeed).Generate(Count))
        {
            yield return new TheoryDataRow<CreatePersonRequest>(r);
        }

        yield return new TheoryDataRow<CreatePersonRequest>(
            new CreatePersonRequest(Guid.Empty, null, null, null, null, null, null, null, null, null, null, null, null));

        yield return new TheoryDataRow<CreatePersonRequest>(
            new CreatePersonRequest(Guid.Empty, string.Empty, string.Empty, string.Empty, null, null, null, null, null, null, null, null, null));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
