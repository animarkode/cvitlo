using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Persons.DeletePerson;

public record DeletePersonCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<bool>>;