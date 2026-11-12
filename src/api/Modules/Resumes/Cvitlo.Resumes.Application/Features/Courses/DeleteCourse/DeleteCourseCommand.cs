using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Courses.DeleteCourse;

public record DeleteCourseCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<bool>>;