using Microsoft.EntityFrameworkCore;
using WareTrack.Api.Data;

namespace WareTrack.Api.Tests;

internal static class ErpTestFactory
{
    public static ErpDbContext CreateDbContext(string name)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        return new ErpDbContext(options);
    }
}
