using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Cvitlo.Resumes.Application.Authorization;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by System.Text.Json when deserializing the realm_access claim.")]
internal sealed class RealmAccess
{
    [JsonPropertyName("roles")]
    public string[] Roles { get; init; } = [];
}
