using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Domain.Entities;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Resumes.Application.Features.Courses.CreateCourse;

public class CreateCourseCommandHandler(
    ResumesDbContext dbContext,
    ILogger<CreateCourseCommandHandler> logger)
    : ICommandHandler<CreateCourseCommand, ErrorOr<CreateCourseResponse>>
{
    private const string HandlerName = nameof(CreateCourseCommandHandler);

    public async Task<ErrorOr<CreateCourseResponse>> ExecuteAsync(CreateCourseCommand command, CancellationToken ct)
    {
        logger.LogInformation("[START] {HandlerName} for resume {ResumeId}", HandlerName, command.ResumeId);

        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            logger.LogWarning("[FAIL] {HandlerName} | Resume {ResumeId} not found", HandlerName, command.ResumeId);

            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var maxOrder = await dbContext.Courses
            .Where(c => c.ResumeId == command.ResumeId)
            .MaxAsync(c => (int?)c.Order, ct);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            ResumeId = command.ResumeId,
            School = command.School,
            Name = command.Name,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Description = HtmlContentSanitizer.SanitizeBasicFormatting(command.Description),
            Order = (maxOrder ?? -1) + 1
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "[SUCCESS] {HandlerName} | Course {CourseId} created for resume {ResumeId}",
            HandlerName,
            course.Id,
            command.ResumeId);

        return course.MapToResponse();
    }
}