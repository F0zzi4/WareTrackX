using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;

namespace MiniErp.Api.Tests;

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
