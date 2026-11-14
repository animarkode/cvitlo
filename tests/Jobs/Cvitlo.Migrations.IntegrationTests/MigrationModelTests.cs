using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Cvitlo.Migrations.IntegrationTests;

// A model change without a migration passes every other test and then fails on the first
// request that touches the new column.
public class MigrationModelTests
{
    private const string UnusedConnectionString = "Host=localhost;Database=unused";

    [Fact]
    public void ResumesModel_ShouldHaveNoChangesWithoutAMigration()
    {
        using var context = TestDbContexts.Resumes(UnusedConnectionString);

        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public void AuthModel_ShouldHaveNoChangesWithoutAMigration()
    {
        using var context = TestDbContexts.Auth(UnusedConnectionString);

        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
