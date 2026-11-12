using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Links.GetAllLinks;

public record GetAllLinksCommand(Guid ResumeId) : ICommand<ErrorOr<GetAllLinksResponse>>;