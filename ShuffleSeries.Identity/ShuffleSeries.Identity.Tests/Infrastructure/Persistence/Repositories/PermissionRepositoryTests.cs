using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Identity.Domain.Entities;
using ShuffleSeries.Identity.Infrastructure.Persistence;
using ShuffleSeries.Identity.Infrastructure.Persistence.Repositories;

namespace ShuffleSeries.Identity.Tests.Infrastructure.Persistence.Repositories;

public class PermissionRepositoryTests
{
    private static IdentityDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new IdentityDbContext(options);
    }

    [Fact]
    public async Task RepositoryOperations_ShouldHandleAddQueryUpdateAndDelete()
    {
        await using var context = CreateInMemoryContext();
        var sut = new PermissionRepository(context);

        var readPerm = Permission.Create("catalog:movies:read", "Catalog", "Read movies");
        var writePerm = Permission.Create("catalog:movies:write", "Catalog", "Write movies");

        sut.Add(readPerm);
        sut.Add(writePerm);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var all = await sut.GetAllAsync();
        all.Should().HaveCount(2);

        var byIds = await sut.GetByIdsAsync([readPerm.Id]);
        byIds.Should().ContainSingle().Which.Code.Should().Be("catalog:movies:read");

        var byCode = await sut.GetByCodeAsync("  CATALOG:MOVIES:WRITE ");
        byCode.Should().NotBeNull();
        byCode!.Id.Should().Be(writePerm.Id);

        var byId = await sut.GetByIdAsync(readPerm.Id);
        byId.Should().NotBeNull();

        context.ChangeTracker.Clear();
        sut.Update(readPerm);
        sut.Delete(readPerm);
        await context.SaveChangesAsync();

        var remaining = await sut.GetAllAsync();
        remaining.Should().ContainSingle().Which.Id.Should().Be(writePerm.Id);
    }
}
