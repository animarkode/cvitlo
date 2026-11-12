using System.Text.Json;
using Cvitlo.Resumes.Application.Features.Courses.CreateCourse;
using Cvitlo.Resumes.Application.Features.Courses.UpdateCourse;
using Cvitlo.Resumes.Application.Features.Educations.CreateEducation;
using Cvitlo.Resumes.Application.Features.Educations.UpdateEducation;
using Cvitlo.Resumes.Application.Features.Experiences.CreateExperience;
using Cvitlo.Resumes.Application.Features.Experiences.UpdateExperience;
using Cvitlo.Resumes.Application.Features.Languages.CreateLanguage;
using Cvitlo.Resumes.Application.Features.Languages.UpdateLanguage;
using Cvitlo.Resumes.Application.Features.Links.CreateLink;
using Cvitlo.Resumes.Application.Features.Links.UpdateLink;
using Cvitlo.Resumes.Application.Features.Persons.CreatePerson;
using Cvitlo.Resumes.Application.Features.Persons.UpdatePerson;
using Cvitlo.Resumes.Application.Features.Skills.CreateSkill;
using Cvitlo.Resumes.Application.Features.Skills.UpdateSkill;
using Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;
using Xunit.Sdk;

[assembly: RegisterXunitSerializer(
    typeof(RecordSerializer),
    typeof(CreatePersonRequest),
    typeof(UpdatePersonRequest),
    typeof(CreateEducationRequest),
    typeof(UpdateEducationRequest),
    typeof(CreateExperienceRequest),
    typeof(UpdateExperienceRequest),
    typeof(CreateSkillRequest),
    typeof(UpdateSkillRequest),
    typeof(CreateLanguageRequest),
    typeof(UpdateLanguageRequest),
    typeof(CreateCourseRequest),
    typeof(UpdateCourseRequest),
    typeof(CreateLinkRequest),
    typeof(UpdateLinkRequest))]

namespace Cvitlo.Resumes.Application.IntegrationTests.Features.Shared;

public class RecordSerializer : IXunitSerializer
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(CreatePersonRequest),
        typeof(UpdatePersonRequest),
        typeof(CreateEducationRequest),
        typeof(UpdateEducationRequest),
        typeof(CreateExperienceRequest),
        typeof(UpdateExperienceRequest),
        typeof(CreateSkillRequest),
        typeof(UpdateSkillRequest),
        typeof(CreateLanguageRequest),
        typeof(UpdateLanguageRequest),
        typeof(CreateCourseRequest),
        typeof(UpdateCourseRequest),
        typeof(CreateLinkRequest),
        typeof(UpdateLinkRequest)
    ];

    public bool IsSerializable(Type type, object? value, out string failureReason)
    {
        if (SupportedTypes.Contains(type))
        {
            failureReason = string.Empty;

            return true;
        }

        failureReason = $"Type '{type.FullName}' is not supported by {nameof(RecordSerializer)}";

        return false;
    }

    public string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, value.GetType());
    }

    public object Deserialize(Type type, string serializedValue)
    {
        return JsonSerializer.Deserialize(serializedValue, type) ??
               throw new InvalidOperationException($"Failed to deserialize '{type.FullName}'");
    }
}
