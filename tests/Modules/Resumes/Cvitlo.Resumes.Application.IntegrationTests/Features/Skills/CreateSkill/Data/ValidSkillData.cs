using System.Collections;
using Cvitlo.Resumes.Application.Features.Skills.CreateSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.Data;
using static Cvitlo.Resumes.Application.IntegrationTests.Features.Shared.ResumesTestConstants;

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Skills.CreateSkill.Data;

public class ValidSkillData : IEnumerable<TheoryDataRow<CreateSkillRequest>>
{
    private const int Count = 3;

    public IEnumerator<TheoryDataRow<CreateSkillRequest>> GetEnumerator()
    {
        foreach (var r in new CreateSkillRequestFaker(Guid.Empty, SkillSeed).Generate(Count))
        {
            yield return new TheoryDataRow<CreateSkillRequest>(r);
        }

        yield return new TheoryDataRow<CreateSkillRequest>(
            new CreateSkillRequest(Guid.Empty, null, null));

        yield return new TheoryDataRow<CreateSkillRequest>(
            new CreateSkillRequest(Guid.Empty, string.Empty, string.Empty));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
