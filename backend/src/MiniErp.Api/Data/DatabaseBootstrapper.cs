using Microsoft.EntityFrameworkCore;

namespace MiniErp.Api.Data;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Database:AutoCreate", true))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await DataSeeder.SeedAsync(db, cancellationToken);
    }
}
