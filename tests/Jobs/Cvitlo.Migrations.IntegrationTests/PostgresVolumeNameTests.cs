using Cvitlo.AppHost;
using Shouldly;

namespace Cvitlo.Migrations.IntegrationTests;

public class PostgresVolumeNameTests
{
    [Fact]
    public void SameCanonicalPath_ShouldProduceStableName()
    {
        var checkoutPath = Path.Combine(Path.GetTempPath(), "cvitlo", "checkout");

        var first = PostgresVolumeName.FromCheckoutPath(checkoutPath);
        var second = PostgresVolumeName.FromCheckoutPath(checkoutPath + Path.DirectorySeparatorChar);

        second.ShouldBe(first);
    }

    [Fact]
    public void DistinctCheckoutPaths_ShouldProduceDistinctNames()
    {
        var first = PostgresVolumeName.FromCheckoutPath(Path.Combine(Path.GetTempPath(), "cvitlo", "checkout-a"));
        var second = PostgresVolumeName.FromCheckoutPath(Path.Combine(Path.GetTempPath(), "cvitlo", "checkout-b"));

        second.ShouldNotBe(first, StringComparer.Ordinal);
    }

    [Fact]
    public void CheckoutPath_ShouldProduceDockerSafeName()
    {
        var checkoutPath = Path.Combine(Path.GetTempPath(), "Cvitlo checkout with spaces", "issue/555");

        var volumeName = PostgresVolumeName.FromCheckoutPath(checkoutPath);

        volumeName.ShouldMatch("^[a-z0-9][a-z0-9_.-]+$");
    }
}
