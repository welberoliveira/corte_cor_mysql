using System.Security.Cryptography;
using System.Text;

namespace CorteCor.Handlers;

public static class PasswordSecurity
{
    private const string Pbkdf2Prefix = "PBKDF2";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int IterationCount = 100_000;

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, IterationCount, HashAlgorithmName.SHA256, KeySize);

        return string.Join(
            '$',
            Pbkdf2Prefix,
            IterationCount.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(key));
    }

    public static bool VerifyPassword(string password, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (IsLegacyBase64Hash(storedHash))
        {
            return string.Equals(storedHash, LegacyEncode(password), StringComparison.Ordinal);
        }

        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], Pbkdf2Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedKey = Convert.FromBase64String(parts[3]);
            var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);
            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool NeedsRehash(string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return true;
        }

        if (IsLegacyBase64Hash(storedHash))
        {
            return true;
        }

        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length != 4 ||
               !int.TryParse(parts[1], out var iterations) ||
               iterations < IterationCount;
    }

    public static string LegacyEncode(string password) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(password));

    private static bool IsLegacyBase64Hash(string storedHash) =>
        !storedHash.StartsWith($"{Pbkdf2Prefix}$", StringComparison.Ordinal);
}
