using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Courses.DeleteCourse;

public class DeleteCourseCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeleteCourseCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeleteCourseCommand command, CancellationToken ct)
    {
        var course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.ResumeId == command.ResumeId, ct);

        if (course is null)
        {
            return Error.NotFound("Course.NotFound", $"Course with id '{command.Id}' was not found");
        }

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}