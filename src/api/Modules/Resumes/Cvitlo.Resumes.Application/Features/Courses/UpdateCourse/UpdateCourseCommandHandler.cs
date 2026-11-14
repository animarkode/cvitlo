using ErrorOr;
using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Courses.UpdateCourse;

public class UpdateCourseCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<UpdateCourseCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(UpdateCourseCommand command, CancellationToken ct)
    {
        var course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.ResumeId == command.ResumeId, ct);

        if (course is null)
        {
            return Error.NotFound("Course.NotFound", $"Course with id '{command.Id}' was not found");
        }

        course.School = command.School;
        course.Name = command.Name;
        course.StartDate = command.StartDate;
        course.EndDate = command.EndDate;
        course.Description = HtmlContentSanitizer.SanitizeBasicFormatting(command.Description);

        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
