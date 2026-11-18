using FastEndpoints;
using FluentValidation;

namespace Cvitlo.Resumes.Application.Features.Courses.ReorderCourses;

public class ReorderCoursesValidator : Validator<ReorderCoursesRequest>
{
    public ReorderCoursesValidator()
    {
        RuleFor(x => x.ResumeId)
            .NotEmpty().WithMessage("'ResumeId' must not be empty.");

        RuleFor(x => x.OrderedIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("'OrderedIds' must not be empty.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("'OrderedIds' must not contain duplicates.");
    }
}
