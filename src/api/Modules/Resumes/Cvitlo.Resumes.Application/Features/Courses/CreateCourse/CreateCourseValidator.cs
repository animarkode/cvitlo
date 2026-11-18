using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using FastEndpoints;
using FluentValidation;

namespace Cvitlo.Resumes.Application.Features.Courses.CreateCourse;

public class CreateCourseValidator : Validator<CreateCourseRequest>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("'Name' must not exceed 100 characters.");

        RuleFor(x => x.School)
            .MaximumLength(100).WithMessage("'School' must not exceed 100 characters.");

        // The raw cap runs first so an oversized body is rejected before the sanitizer parses it.
        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(1000).WithMessage("'Description' must not exceed 1000 characters.")
            .Must(description => HtmlContentSanitizer.GetSanitizedLength(description) <= 1000)
            .WithMessage("'Description' must not exceed 1000 characters.");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("'StartDate' must not be in the future.")
            .When(x => x.StartDate.HasValue);

        RuleFor(x => x.EndDate)
            .Must((r, endDate) => endDate >= r.StartDate)
            .WithMessage("'EndDate' must be greater than or equal to 'StartDate'.")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
