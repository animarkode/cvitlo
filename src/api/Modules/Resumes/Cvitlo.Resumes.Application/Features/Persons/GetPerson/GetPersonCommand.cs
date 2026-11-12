using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Persons.GetPerson;

public record GetPersonCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<GetPersonResponse>>;