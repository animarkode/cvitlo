using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using FastEndpoints;
using FluentValidation;
using static Cvitlo.Resumes.Infrastructure.Database.DbConstants;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateSummary;

public class UpdateSummaryValidator : Validator<UpdateSummaryRequest>
{
    public UpdateSummaryValidator()
    {
        // The column stores sanitized markup, so that length is what must fit. The raw cap only keeps
        // huge bodies away from the parser: it stays loose because the sanitizer drops attributes the
        // editor adds to every link (target, rel).
        RuleFor(x => x.Summary)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("'Summary' is required.")
            .MaximumLength(SummaryMaxLength * 4).WithMessage($"'Summary' markup must not exceed {SummaryMaxLength} characters.")
            .Must(value => HtmlContentSanitizer.GetPlainTextLength(value) <= 200)
            .WithMessage("'Summary' must not exceed 200 characters.")
            .Must(value => HtmlContentSanitizer.GetSanitizedLength(value) <= SummaryMaxLength)
            .WithMessage($"'Summary' markup must not exceed {SummaryMaxLength} characters.");
    }
}
