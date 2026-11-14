using ErrorOr;
using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Persons.CreatePerson;

public class CreatePersonEndpoint
    : Endpoint<CreatePersonRequest, Results<Ok<CreatePersonResponse>, NotFound, Conflict>>
{
    public override void Configure()
    {
        Post("");
        Group<PersonsSubGroup>();
        Version(1);
        Policies("ResumeOwnerPolicy");
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<Ok<CreatePersonResponse>, NotFound, Conflict>> ExecuteAsync(
        CreatePersonRequest req,
        CancellationToken ct)
    {
        var result = await req.MapToCommand().ExecuteAsync(ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.Conflict)
            {
                return TypedResults.Conflict();
            }

            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result.Value);
    }
}
