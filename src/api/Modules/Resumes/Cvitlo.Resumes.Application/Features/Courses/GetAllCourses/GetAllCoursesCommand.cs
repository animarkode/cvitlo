using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Courses.GetAllCourses;

public record GetAllCoursesCommand(Guid ResumeId) : ICommand<ErrorOr<GetAllCoursesResponse>>;