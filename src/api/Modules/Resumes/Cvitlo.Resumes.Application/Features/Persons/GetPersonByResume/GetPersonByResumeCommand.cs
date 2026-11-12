using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Persons.GetPersonByResume;

public record GetPersonByResumeCommand(Guid ResumeId) : ICommand<ErrorOr<GetPersonByResumeResponse>>;