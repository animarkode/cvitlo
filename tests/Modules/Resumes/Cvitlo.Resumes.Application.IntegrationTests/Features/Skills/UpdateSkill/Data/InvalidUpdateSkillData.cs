using System.Collections;
using Cvitlo.Resumes.Application.Features.Skills.UpdateSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Skills.UpdateSkill.Data;

public class InvalidUpdateSkillData : IEnumerable<TheoryDataRow<UpdateSkillRequest, string>>
{
    public IEnumerator<TheoryDataRow<UpdateSkillRequest, string>> GetEnumerator()
    {
        var source = new CreateSkillRequestFaker(Guid.Empty).Generate();
        var baseRequest = new UpdateSkillRequest(Guid.NewGuid(), Guid.Empty, source.Name, source.Level);

        yield return new TheoryDataRow<UpdateSkillRequest, string>(
            baseRequest with { Level = "InvalidLevel" },
            "level");

        yield return new TheoryDataRow<UpdateSkillRequest, string>(
            baseRequest with { Level = "beginner" },
            "level");

        yield return new TheoryDataRow<UpdateSkillRequest, string>(
            baseRequest with { Name = new string('a', 101) },
            "name");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
