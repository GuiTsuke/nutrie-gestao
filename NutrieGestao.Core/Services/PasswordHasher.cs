using System.Security.Cryptography;

namespace NutrieGestao.Services;

public static class PasswordHasher
{
    public const int DefaultIterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static PasswordHash Criar(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, DefaultIterations, HashAlgorithmName.SHA256, HashSize);

        return new PasswordHash(
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt),
            DefaultIterations);
    }

    public static bool Verificar(string senha, string hashBase64, string saltBase64, int iteracoes)
    {
        if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hashBase64) ||
            string.IsNullOrWhiteSpace(saltBase64) || iteracoes <= 0)
        {
            return false;
        }

        try
        {
            var expectedHash = Convert.FromBase64String(hashBase64);
            var salt = Convert.FromBase64String(saltBase64);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record PasswordHash(string Hash, string Salt, int Iterations);
