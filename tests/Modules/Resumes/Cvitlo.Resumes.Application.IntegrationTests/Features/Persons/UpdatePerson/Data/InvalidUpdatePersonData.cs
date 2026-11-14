using System.Collections;
using Cvitlo.Resumes.Application.Features.Persons.UpdatePerson;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Persons.UpdatePerson.Data;

public class InvalidUpdatePersonData : IEnumerable<TheoryDataRow<UpdatePersonRequest, string>>
{
    private static readonly string TooLong = new('a', 101);

    private static readonly (Func<UpdatePersonRequest, UpdatePersonRequest> Apply, string Key)[] Violations =
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

    public IEnumerator<TheoryDataRow<UpdatePersonRequest, string>> GetEnumerator()
    {
        var source = new CreatePersonRequestFaker(Guid.Empty).Generate();
        var baseRequest = new UpdatePersonRequest(
            Guid.NewGuid(),
            Guid.Empty,
            source.JobTitle,
            source.Firstname,
            source.Lastname,
            source.Email,
            source.Phone,
            source.Country,
            source.City,
            source.Street,
            source.PostCode,
            source.Nationality,
            source.DateOfBirth,
            source.DrivingLicense);

        foreach (var (apply, key) in Violations)
        {
            yield return new TheoryDataRow<UpdatePersonRequest, string>(apply(baseRequest), key);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
