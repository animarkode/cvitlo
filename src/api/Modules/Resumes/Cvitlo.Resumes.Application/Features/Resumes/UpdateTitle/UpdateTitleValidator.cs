using FastEndpoints;
using FluentValidation;

namespace Cvitlo.Resumes.Application.Features.Resumes.UpdateTitle;

public class UpdateTitleValidator : Validator<UpdateTitleRequest>
{
    public UpdateTitleValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("'Title' is required.")
            .Must(value => value.Trim().Length <= 100)
            .WithMessage("'Title' must not exceed 100 characters.");
    }
}
