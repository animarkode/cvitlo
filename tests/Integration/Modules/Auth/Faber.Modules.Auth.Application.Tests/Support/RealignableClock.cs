using Microsoft.Extensions.Time.Testing;

namespace Faber.Modules.Auth.Application.Tests.Support;

/// <summary>
/// The API's clock. Tests move it forward to make tokens look expired, but Keycloak keeps real time, so a
/// sign-in made after such a jump looks hours old to the API (its <c>auth_time</c> is real). A test that
/// needs a fresh sign-in calls <see cref="Realign"/> first; a <see cref="FakeTimeProvider"/> itself cannot go back.
/// </summary>
public sealed class RealignableClock(DateTimeOffset start) : FakeTimeProvider(start)
{
    private TimeSpan _behind = TimeSpan.Zero;

    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() - _behind;

    /// <summary>Makes the clock read the real time again; it keeps being advanceable from there.</summary>
    public void Realign()
    {
        _behind = TimeSpan.Zero;
        _behind = base.GetUtcNow() - DateTimeOffset.UtcNow;
    }
}
