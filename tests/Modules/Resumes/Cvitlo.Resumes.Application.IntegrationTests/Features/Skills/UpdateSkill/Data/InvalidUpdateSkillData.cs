using System.Collections;
using Cvitlo.Resumes.Application.Features.Skills.UpdateSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Skills.UpdateSkill.Data;

public class InvalidUpdateSkillData : IEnumerable<TheoryDataRow<UpdateSkillRequest>>
{
    public IEnumerator<TheoryDataRow<UpdateSkillRequest>> GetEnumerator()
    {
        var baseFaker = new CreateSkillRequestFaker(Guid.Empty);

        yield return new TheoryDataRow<UpdateSkillRequest>(
            new UpdateSkillRequest(Guid.Empty, Guid.Empty, new string('a', 256),
                baseFaker.Generate().Level));

        yield return new TheoryDataRow<UpdateSkillRequest>(
            new UpdateSkillRequest(Guid.Empty, Guid.Empty, baseFaker.Generate().Name,
                "InvalidLevel"));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
