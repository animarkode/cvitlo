using Cvitlo.Auth.PublicApi;

namespace Cvitlo.Resumes.Application.Options;

public class ResumeLimitsOptions
{
    public IDictionary<Role, int> Limits { get; set; } = new Dictionary<Role, int> { { Role.Regular, 1 } };
}
