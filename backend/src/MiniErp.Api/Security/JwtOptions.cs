using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MiniErp.Api.Security;

public sealed record JwtOptions(
    string Issuer,
    string Audience,
    string SigningKey,
    int ExpirationMinutes)
{
    public const string SectionName = "Jwt";

    public static JwtOptions DevelopmentDefaults { get; } = new(
        "MiniErp.Api",
        "MiniErp.Frontend",
        "local-development-signing-key-for-mini-erp-portfolio-demo",
        120);

    public SymmetricSecurityKey CreateSigningKey() =>
        new(Encoding.UTF8.GetBytes(SigningKey));
}
