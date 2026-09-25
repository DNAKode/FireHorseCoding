using System.Security.Cryptography;

namespace KodeWork.Core;

/// <summary>Stable KodeWork identity: <c>kw:</c> plus a Crockford ULID.</summary>
public readonly record struct ItemId(string Value)
{
    public static ItemId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(Kw.IdPrefix, StringComparison.Ordinal) || value.Length < 8)
        {
            throw new ArgumentException($"Not a KodeWork id: '{value}'", nameof(value));
        }
        return new ItemId(value);
    }

    public static ItemId New() => new(Kw.IdPrefix + Ulid.New());

    public string FileStem => Value.Replace(":", "_", StringComparison.Ordinal);

    public override string ToString() => Value;
}

internal static class Ulid
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string New()
    {
        long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Span<byte> rnd = stackalloc byte[10];
        RandomNumberGenerator.Fill(rnd);

        Span<char> chars = stackalloc char[26];
        EncodeTime(ms, chars);
        EncodeRandom(rnd, chars);
        return new string(chars);
    }

    private static void EncodeTime(long ms, Span<char> chars)
    {
        for (int i = 9; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(ms % 32)];
            ms /= 32;
        }
    }

    private static void EncodeRandom(ReadOnlySpan<byte> rnd, Span<char> chars)
    {
        // 80 bits into 16 * 5-bit chars starting at index 10.
        int acc = 0;
        int bits = 0;
        int o = 10;
        foreach (byte b in rnd)
        {
            acc = (acc << 8) | b;
            bits += 8;
            while (bits >= 5 && o < 26)
            {
                chars[o++] = Alphabet[(acc >> (bits - 5)) & 31];
                bits -= 5;
            }
        }
        if (o < 26)
        {
            chars[o] = Alphabet[(acc << (5 - bits)) & 31];
        }
    }
}
