using MiniErp.Api.Data;
using MiniErp.Api.Services;

namespace MiniErp.Api.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_returns_enterprise_operational_metrics()
    {
        await using var db = ErpTestFactory.CreateDbContext(nameof(GetSummaryAsync_returns_enterprise_operational_metrics));
        await DataSeeder.SeedAsync(db);

        var summary = await new DashboardService(db).GetSummaryAsync();

        Assert.Equal(4, summary.Kpis.Count);
        Assert.Contains(summary.Kpis, metric => metric.Label == "Revenue");
        Assert.NotEmpty(summary.CategoryRevenue);
        Assert.NotEmpty(summary.RecentOrders);
    }
}
