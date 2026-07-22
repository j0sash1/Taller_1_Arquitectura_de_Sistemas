using System.Security.Cryptography;
using System.Text;

namespace Shortly.Infrastructure;

public static class ShortCodeGenerator
{
    /// <summary>
    /// Generates a URL-safe identifier without exposing the timestamp
    /// contained in the original ULID. This prevents clients from
    /// inferring link creation time while preserving uniqueness.
    /// </summary>
    public static string Generate()
    {
        var ulid = Ulid.NewUlid().ToString();

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(ulid));

        return Convert.ToHexString(hash)
            .ToLowerInvariant()[..12];
    }
}