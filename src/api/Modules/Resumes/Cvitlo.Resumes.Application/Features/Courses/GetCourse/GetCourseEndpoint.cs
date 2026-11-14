using Cvitlo.Common.RateLimiting;
using Cvitlo.Resumes.Application.Groups;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cvitlo.Resumes.Application.Features.Courses.GetCourse;

public class GetCourseEndpoint
    : Endpoint<GetCourseRequest, Results<Ok<GetCourseResponse>, NotFound, UnauthorizedHttpResult>>
{
    public override void Configure()
    {
        Get("{Id}");
        Group<CoursesSubGroup>();
        Version(1);
        Policies("ResumeOwnerPolicy");
        Options(x => x.RequireRateLimiting(RateLimitPolicies.AuthenticatedDefault));
    }

    public override async Task<Results<Ok<GetCourseResponse>, NotFound, UnauthorizedHttpResult>> ExecuteAsync(
        GetCourseRequest req,
        CancellationToken ct)
    {
        var result = await req.MapToCommand().ExecuteAsync(ct);

        if (result.IsError)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result.Value);
    }
}