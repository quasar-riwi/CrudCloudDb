using System.Security.Cryptography;

namespace CrudCloud.api.Utils;

public class TokenGenerator
{
    public static string GenerateToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToHexString(randomBytes);
    }
    
    public static DateTime GenerateExpirationDate(int hours)
    {
        return DateTime.UtcNow.AddHours(hours);
    }
}