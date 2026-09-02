using System.Security.Cryptography;

namespace MiniErp.Api.Security;

public static class PasswordHasher
{
    private const int Iterations = 120_000;
    private const int KeySize = 32;

    public static string CreateSalt()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }

    public static string Hash(string password, string salt)
    {
        var saltBytes = Convert.FromHexString(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return Convert.ToHexString(hash);
    }

    public static bool Verify(string password, string salt, string expectedHash)
    {
        var hash = Hash(password, salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(hash),
            Convert.FromHexString(expectedHash));
    }
}
