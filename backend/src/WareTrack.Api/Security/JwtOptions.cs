using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WareTrack.Api.Security;

public sealed record JwtOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    int ExpirationMinutes)
{
    public const string SectionName = "Jwt";

    public static JwtOptions DevelopmentDefaults { get; } = new(
        "WareTrack.Api",
        "WareTrack.Frontend",
        "local-development-signing-key-for-waretrack-portfolio-demo",
        120);

    public SymmetricSecurityKey CreateSigningKey() =>
        new(Encoding.UTF8.GetBytes(SigningKey));
}
