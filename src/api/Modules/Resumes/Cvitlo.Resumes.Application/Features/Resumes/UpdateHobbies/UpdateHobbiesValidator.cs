using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using FastEndpoints;
using FluentValidation;
using static Cvitlo.Resumes.Infrastructure.Database.DbConstants;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateHobbies;

public class UpdateHobbiesValidator : Validator<UpdateHobbiesRequest>
{
    public UpdateHobbiesValidator()
    {
        // The column stores sanitized markup, so that length is what must fit. The raw cap only keeps
        // huge bodies away from the parser: it stays loose because the sanitizer drops attributes the
        // editor adds to every link (target, rel).
        RuleFor(x => x.Hobbies)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("'Hobbies' is required.")
            .MaximumLength(HobbiesMaxLength * 4).WithMessage($"'Hobbies' markup must not exceed {HobbiesMaxLength} characters.")
            .Must(value => HtmlContentSanitizer.GetPlainTextLength(value) <= 200)
            .WithMessage("'Hobbies' must not exceed 200 characters.")
            .Must(value => HtmlContentSanitizer.GetSanitizedLength(value) <= HobbiesMaxLength)
            .WithMessage($"'Hobbies' markup must not exceed {HobbiesMaxLength} characters.");
    }
}
