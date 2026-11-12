using ErrorOr;
using FastEndpoints;

namespace Cvitlo.Resumes.Application.Features.Courses.ReorderCourses;

public record ReorderCoursesCommand(
    Guid ResumeId,
    IReadOnlyList<Guid> OrderedIds) : ICommand<ErrorOr<bool>>;
