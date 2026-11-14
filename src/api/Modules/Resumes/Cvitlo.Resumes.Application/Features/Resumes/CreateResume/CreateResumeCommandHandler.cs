using ErrorOr;
using Cvitlo.Resumes.Domain.Entities;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Resumes.CreateResume;

public class CreateResumeCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreateResumeCommand, ErrorOr<CreateResumeResponse>>
{
    public async Task<ErrorOr<CreateResumeResponse>> ExecuteAsync(CreateResumeCommand command, CancellationToken ct)
    {
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            CreatedAt = DateTimeOffset.UtcNow,
            Localization = command.Localization
        };

        dbContext.Resumes.Add(resume);
        await dbContext.SaveChangesAsync(ct);

        return resume.MapToResponse();
    }
}