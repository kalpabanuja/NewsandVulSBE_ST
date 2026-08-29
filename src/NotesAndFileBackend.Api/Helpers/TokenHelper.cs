using System.Security.Cryptography;
using System.Text;

namespace NotesAndFileBackend.Api.Helpers;

public static class TokenHelper
{
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string GenerateToken(string? alias)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            // Clean alias (only allow alphanumeric and hyphens/underscores)
            var cleanAlias = new string(alias.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            if (string.IsNullOrWhiteSpace(cleanAlias)) cleanAlias = "share";

            // Generate a random 4-digit number
            int randomNumber = RandomNumberGenerator.GetInt32(1000, 10000);
            return $"{cleanAlias}_{randomNumber}";
        }
        else
        {
            // Secure 32-character base62 string
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var sb = new StringBuilder(32);
            foreach (var b in randomBytes)
            {
                sb.Append(Base62Chars[b % 62]);
            }
            return sb.ToString();
        }
    }
}

