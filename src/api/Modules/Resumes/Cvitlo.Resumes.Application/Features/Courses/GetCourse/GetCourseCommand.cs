using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Courses.GetCourse;

public record GetCourseCommand(Guid ResumeId, Guid Id) : ICommand<ErrorOr<GetCourseResponse>>;