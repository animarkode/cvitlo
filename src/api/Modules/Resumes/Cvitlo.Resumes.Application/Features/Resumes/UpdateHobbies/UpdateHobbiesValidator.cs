using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using FastEndpoints;
using FluentValidation;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateHobbies;

public class UpdateHobbiesValidator : Validator<UpdateHobbiesRequest>
{
    public UpdateHobbiesValidator()
    {
        // The column stores sanitized markup, so its length is capped too; the raw cap runs first
        // so an oversized body is rejected before the sanitizer parses it.
        RuleFor(x => x.Hobbies)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("'Hobbies' is required.")
            .MaximumLength(1000).WithMessage("'Hobbies' markup must not exceed 1000 characters.")
            .Must(value => HtmlContentSanitizer.GetPlainTextLength(value) <= 200)
            .WithMessage("'Hobbies' must not exceed 200 characters.")
            .Must(value => HtmlContentSanitizer.GetSanitizedLength(value) <= 1000)
            .WithMessage("'Hobbies' markup must not exceed 1000 characters.");
    }
}
