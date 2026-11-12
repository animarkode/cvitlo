using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Links.CreateLink;

public record CreateLinkCommand(
    Guid ResumeId,
    string? Label,
    string? Uri) : ICommand<ErrorOr<CreateLinkResponse>>;