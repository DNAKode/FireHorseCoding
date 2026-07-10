using System.Security.Cryptography;
using System.Text;

namespace KodePorter.Core.Hashing;

/// <summary>Small sha256 helpers shared by tree hashing, entity ids, and basis ids.</summary>
public static class Sha256Util
{
    /// <summary>Lowercase hex sha256 of the given bytes.</summary>
    public static string HexOfBytes(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>Lowercase hex sha256 of the UTF-8 encoding of <paramref name="text"/>.</summary>
    public static string HexOfUtf8(string text) => HexOfBytes(Encoding.UTF8.GetBytes(text));

    /// <summary>Lowercase hex sha256 of the raw bytes of the file at <paramref name="path"/>.</summary>
    public static string HexOfFile(string path) => HexOfBytes(File.ReadAllBytes(path));
}
