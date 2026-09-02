using WareTrack.Api.Contracts;
using WareTrack.Api.Data;
using WareTrack.Api.Security;
using WareTrack.Api.Services;

namespace WareTrack.Api.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_returns_profile_and_token_for_valid_demo_user()
    {
        await using var db = ErpTestFactory.CreateDbContext(nameof(AuthenticateAsync_returns_profile_and_token_for_valid_demo_user));
        await DataSeeder.SeedAsync(db);

        var options = JwtOptions.DevelopmentDefaults;
        var service = new AuthService(db, new JwtTokenService(options), options);

        var response = await service.AuthenticateAsync(new LoginRequest(
            "victor.siqueira@enterprise.dev",
            DataSeeder.DemoPassword));

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Equal("Victor Siqueira", response.User.Name);
        Assert.Equal("Administrator", response.User.Role.ToString());
    }

    [Fact]
    public async Task AuthenticateAsync_rejects_invalid_password()
    {
        await using var db = ErpTestFactory.CreateDbContext(nameof(AuthenticateAsync_rejects_invalid_password));
        await DataSeeder.SeedAsync(db);

        var options = JwtOptions.DevelopmentDefaults;
        var service = new AuthService(db, new JwtTokenService(options), options);

        var response = await service.AuthenticateAsync(new LoginRequest(
            "victor.siqueira@enterprise.dev",
            "wrong-password"));

        Assert.Null(response);
    }
}
