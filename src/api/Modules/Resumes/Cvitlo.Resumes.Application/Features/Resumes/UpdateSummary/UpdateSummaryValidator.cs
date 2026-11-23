using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using FastEndpoints;
using FluentValidation;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateSummary;

public class UpdateSummaryValidator : Validator<UpdateSummaryRequest>
{
    public UpdateSummaryValidator()
    {
        // The column stores sanitized markup, so its length is capped too; the raw cap runs first
        // so an oversized body is rejected before the sanitizer parses it.
        RuleFor(x => x.Summary)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("'Summary' is required.")
            .MaximumLength(2000).WithMessage("'Summary' markup must not exceed 2000 characters.")
            .Must(value => HtmlContentSanitizer.GetPlainTextLength(value) <= 200)
            .WithMessage("'Summary' must not exceed 200 characters.")
            .Must(value => HtmlContentSanitizer.GetSanitizedLength(value) <= 2000)
            .WithMessage("'Summary' markup must not exceed 2000 characters.");
    }
}
