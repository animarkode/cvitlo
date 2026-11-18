using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Cvitlo.Common.Testing.Http;

public static class ProblemResponseExtensions
{
    /// <summary>Reads the <c>code</c> extension of a problem-details response, or null when there is none.</summary>
    public static async Task<string?> ReadProblemCodeAsync(this HttpResponseMessage response, CancellationToken ct)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(ct);

        return problem?["code"]?.GetValue<string>();
    }
}
