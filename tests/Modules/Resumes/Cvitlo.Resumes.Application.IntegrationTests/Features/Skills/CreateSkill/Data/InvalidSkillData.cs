using System.Collections;
using Cvitlo.Resumes.Application.Features.Skills.CreateSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Skills.CreateSkill.Data;

public class InvalidSkillData : IEnumerable<TheoryDataRow<CreateSkillRequest, string>>
{
    public IEnumerator<TheoryDataRow<CreateSkillRequest, string>> GetEnumerator()
    {
        var baseRequest = new CreateSkillRequestFaker(Guid.NewGuid()).Generate();

        yield return new TheoryDataRow<CreateSkillRequest, string>(
            baseRequest with { Level = "InvalidLevel" },
            "level");

        yield return new TheoryDataRow<CreateSkillRequest, string>(
            baseRequest with { Level = "beginner" },
            "level");

        yield return new TheoryDataRow<CreateSkillRequest, string>(
            baseRequest with { Name = new string('a', 101) },
            "name");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
