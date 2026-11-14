using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Domain.Entities;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Courses.CreateCourse;

public class CreateCourseCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreateCourseCommand, ErrorOr<CreateCourseResponse>>
{
    public async Task<ErrorOr<CreateCourseResponse>> ExecuteAsync(CreateCourseCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
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

        return course.MapToResponse();
    }
}