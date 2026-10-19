using System.Buffers.Binary;
using Microsoft.AspNetCore.WebUtilities;

namespace NutrieGestao.Web.Infrastructure;

internal static class AntiforgeryDiagnostics
{
    // Registra somente a estrutura do token, nunca o valor que dá acesso ao formulário.
    public static object Describe(string? token)
    {
        var encodedLength = token?.Length ?? 0;
        if (string.IsNullOrEmpty(token))
            return new { EncodedLength = encodedLength, Format = "Missing" };

        try
        {
            var bytes = WebEncoders.Base64UrlDecode(token);
            var validHeader = bytes.Length >= 20 &&
                BinaryPrimitives.ReadUInt32BigEndian(bytes) == 0x09F0C9F0;

            return new
            {
                EncodedLength = encodedLength,
                DecodedLength = bytes.Length,
                Format = validHeader ? "DataProtection" : "InvalidHeader",
                KeyId = validHeader ? new Guid(bytes.AsSpan(4, 16)).ToString() : null
            };
        }
        catch (FormatException)
        {
            return new { EncodedLength = encodedLength, Format = "InvalidEncoding" };
        }
    }
}
