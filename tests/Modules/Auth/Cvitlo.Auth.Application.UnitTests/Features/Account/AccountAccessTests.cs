using ErrorOr;
using Cvitlo.Auth.Application.Features.Account;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Cvitlo.Auth.Application.UnitTests.Features.Account;

public class AccountAccessTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Failure, 502)]
    [InlineData(ErrorType.Unexpected, 502)]
    public void ToProblem_ErrorType_ShouldMapToStatus(ErrorType type, int status)
    {
        var error = Error.Custom((int)type, "Some.Code", "described");

        var problem = error.ToProblem();

        problem.StatusCode.ShouldBe(status);
        if (type != ErrorType.Validation)
        {
            problem.ProblemDetails.Extensions["code"].ShouldBe("Some.Code");
        }
    }

    [Fact]
    public void ToProblem_ValidationWithFieldMetadata_ShouldReturnValidationProblemPerField()
    {
        var error = Error.Validation("Account.Validation", "invalid", new Dictionary<string, object>(StringComparer.Ordinal) { ["firstName"] = "too long" });

        var problem = error.ToProblem();

        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var details = problem.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();
        details.Errors["firstName"].ShouldBe(["too long"]);
    }

    [Fact]
    public void ToProblem_ValidationWithoutMetadata_ShouldReturnPlainBadRequest()
    {
        var problem = Error.Validation("Account.Validation", "invalid").ToProblem();

        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.ShouldNotBeOfType<HttpValidationProblemDetails>();
    }
}
