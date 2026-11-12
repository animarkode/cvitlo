using System.Diagnostics.CodeAnalysis;

namespace Cvitlo.Resumes.PublicApi;

[SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "Module marker contract; cross-module operations will be added here.")]
public interface IResumesModuleApi;
