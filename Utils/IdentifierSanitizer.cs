using System.Text.RegularExpressions;

namespace CrudCloud.api.Utils;

public class IdentifierSanitizer
{
    // permite letras, numeros y underscore, min 1 char
    private static readonly Regex ValidId = new(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Identifier vacío");
        // remover chars no permitidos, limitar length
        var cleaned = ValidId.Replace(input, "");
        if (cleaned.Length > 50) cleaned = cleaned.Substring(0, 50);
        if (string.IsNullOrWhiteSpace(cleaned)) throw new ArgumentException("Identifier inválido después de sanitizar");
        return cleaned.ToLower();
    }
}