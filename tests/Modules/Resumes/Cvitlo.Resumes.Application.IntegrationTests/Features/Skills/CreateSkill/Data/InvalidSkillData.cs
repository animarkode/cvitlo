using System.Collections;
using Cvitlo.Resumes.Application.Features.Skills.CreateSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Skills.CreateSkill.Data;

public class InvalidSkillData : IEnumerable<TheoryDataRow<CreateSkillRequest>>
{
    public IEnumerator<TheoryDataRow<CreateSkillRequest>> GetEnumerator()
    {
        var baseFaker = new CreateSkillRequestFaker(Guid.NewGuid());

        yield return new TheoryDataRow<CreateSkillRequest>(
            baseFaker.RuleFor(r => r.Level, "InvalidLevel").Generate());

        yield return new TheoryDataRow<CreateSkillRequest>(
            baseFaker.RuleFor(r => r.Name, new string('a', 256)).Generate());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
