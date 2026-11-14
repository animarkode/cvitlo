using System.Collections;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Persons.CreatePerson.Data;

public class InvalidPersonData : IEnumerable<TheoryDataRow<CreatePersonRequest, string>>
{
    private static readonly string TooLong = new('a', 101);

    private static readonly (Func<CreatePersonRequest, CreatePersonRequest> Apply, string Key)[] Violations =
    [
        (r => r with { Email = "invalid-email" }, "email"),
        (r => r with { Email = "invalid email example.com" }, "email"),
        (r => r with { Email = new string('a', 92) + "@test.com" }, "email"),
        (r => r with { JobTitle = TooLong }, "jobTitle"),
        (r => r with { Firstname = TooLong }, "firstname"),
        (r => r with { Lastname = TooLong }, "lastname"),
        (r => r with { Phone = TooLong }, "phone"),
        (r => r with { Country = TooLong }, "country"),
        (r => r with { City = TooLong }, "city"),
        (r => r with { Street = TooLong }, "street"),
        (r => r with { PostCode = TooLong }, "postCode"),
        (r => r with { Nationality = TooLong }, "nationality"),
        (r => r with { DrivingLicense = TooLong }, "drivingLicense"),
        (r => r with { DateOfBirth = new DateOnly(2099, 1, 1) }, "dateOfBirth")
    ];

    public IEnumerator<TheoryDataRow<CreatePersonRequest, string>> GetEnumerator()
    {
        var baseRequest = new CreatePersonRequestFaker(Guid.NewGuid()).Generate();

        foreach (var (apply, key) in Violations)
        {
            yield return new TheoryDataRow<CreatePersonRequest, string>(apply(baseRequest), key);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
