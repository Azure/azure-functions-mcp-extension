using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;


namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal class Utility
{
    internal static string CreateId()
    {
        Span<byte> buffer = stackalloc byte[16];
        RandomNumberGenerator.Fill(buffer);
        return WebEncoders.Base64UrlEncode(buffer);
    }
}