using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Links.DeleteLink;

public record DeleteLinkCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<bool>>;