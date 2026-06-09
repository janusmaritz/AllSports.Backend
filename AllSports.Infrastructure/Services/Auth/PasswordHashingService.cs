using AllSports.Application.Interfaces.Auth.Services;
using System.Security.Cryptography;

namespace AllSports.Infrastructure.Services.Auth;

public class PasswordHashingService : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 350_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var parts = hashedPassword.Split(':');
        if (parts.Length != 2) return false;

        var salt = Convert.FromHexString(parts[0]);
        var expectedHash = Convert.FromHexString(parts[1]);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, Iterations, Algorithm, HashSize);

        return CryptographicOperations.FixedTimeEquals(expectedHash, computedHash);
    }
}
