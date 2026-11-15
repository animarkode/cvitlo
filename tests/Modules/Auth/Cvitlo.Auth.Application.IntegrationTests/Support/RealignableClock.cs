using Microsoft.Extensions.Time.Testing;

namespace Cvitlo.Auth.Application.IntegrationTests.Support;

// The API's clock. Tests move it forward to make tokens look expired, but Keycloak keeps real time, so a
// sign-in made after such a jump looks hours old to the API (its auth_time is real). A test that
// needs a fresh sign-in calls Realign first; a FakeTimeProvider itself cannot go back.
public sealed class RealignableClock(DateTimeOffset start) : FakeTimeProvider(start)
{
    private TimeSpan _behind = TimeSpan.Zero;

    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() - _behind;

    public void Realign()
    {
        _behind = TimeSpan.Zero;
        _behind = base.GetUtcNow() - DateTimeOffset.UtcNow;
    }
}
