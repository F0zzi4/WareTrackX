using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Contracts;
using MiniErp.Api.Data;
using MiniErp.Api.Security;

namespace MiniErp.Api.Services;

public sealed class AuthService(
    ErpDbContext db,
    JwtTokenService tokens,
    JwtOptions options)
{
    public async Task<AuthResponse?> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(
            candidate => candidate.Email.ToLower() == email && candidate.IsActive,
            cancellationToken);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordSalt, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            tokens.IssueToken(user),
            DateTimeOffset.UtcNow.AddMinutes(options.ExpirationMinutes),
            user.ToProfileDto());
    }
}
