using System.Text.RegularExpressions;
using Shouldly;

namespace Cvitlo.ArchitectureTests;

public partial class TestNamingTests
{
    // Classes whose tests check configuration or an invariant, so they have no condition part.
    private static readonly HashSet<string> ConfigurationClasses = new(StringComparer.Ordinal)
    {
        "AppHostModelTests", "CvitloArchitectureTests", "LayerTests", "ModuleBoundaryTests",
        "ProjectReferenceTests", "SharedKernelTests", "MigrationModelTests", "RealmSecurityTests",
        "ResumesRateLimitingCoverageTests", "EndpointRateLimitingCoverageTests",
        "RateLimitPolicyRegistrationTests", "TemplateStringsTests", "SecurityHeadersTests", "TestNamingTests",
    };

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*_([A-Z0-9][A-Za-z0-9]*_)?Should[A-Z][A-Za-z0-9]*$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ConventionName();

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*_Should[A-Z][A-Za-z0-9]*$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TwoPartName();

    [Fact]
    public void TestClasses_ShouldEndInTests()
    {
        var offenders = TestMethodScanner.Methods
            .Where(m => !m.Class.EndsWith("Tests", StringComparison.Ordinal))
            .Select(m => $"{m.File}: {m.Class}")
            .Distinct(StringComparer.Ordinal);

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void TestMethods_ShouldFollowSubjectConditionShouldOutcome()
    {
        var offenders = TestMethodScanner.Methods
            .Where(m => !ConventionName().IsMatch(m.Method))
            .Select(Describe);

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void TwoPartTestNames_ShouldBeLimitedToConfigurationClasses()
    {
        var offenders = TestMethodScanner.Methods
            .Where(m => TwoPartName().IsMatch(m.Method) && !ConfigurationClasses.Contains(m.Class))
            .Select(Describe);

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void TestAttributes_ShouldNotSetDisplayName()
    {
        var offenders = TestMethodScanner.Methods.Where(m => m.HasDisplayName).Select(Describe);

        offenders.ShouldBeEmpty();
    }

    private static string Describe(TestMethod method) => $"{method.File}: {method.Class}.{method.Method}";
}
