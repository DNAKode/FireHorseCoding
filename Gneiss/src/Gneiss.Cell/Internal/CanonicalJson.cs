using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Gneiss.Cell.Internal;

/// <summary>
/// Hand-rolled canonical JSON writer per CONTRACT.md section 5: UTF-8, no whitespace, object keys
/// in fixed schema-defined order (never alphabetical-at-runtime), numbers as invariant strings.
/// This is NOT a general-purpose JSON library — it only supports the shapes Gneiss.Cell needs to
/// hash or persist deterministically.
/// </summary>
internal sealed class CanonicalJsonWriter
{
    private readonly StringBuilder _sb = new();
    private bool _needsComma;

    internal static string ToJson(Action<CanonicalJsonWriter> write)
    {
        var w = new CanonicalJsonWriter();
        write(w);
        return w._sb.ToString();
    }

    private void Comma()
    {
        if (_needsComma)
        {
            _sb.Append(',');
        }
        _needsComma = false;
    }

    internal CanonicalJsonWriter Obj(Action<CanonicalJsonWriter> body)
    {
        Comma();
        _sb.Append('{');
        var inner = new CanonicalJsonWriter();
        body(inner);
        _sb.Append(inner._sb);
        _sb.Append('}');
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter Arr<T>(IEnumerable<T> items, Action<CanonicalJsonWriter, T> each)
    {
        Comma();
        _sb.Append('[');
        var inner = new CanonicalJsonWriter();
        foreach (var item in items)
        {
            each(inner, item);
        }
        _sb.Append(inner._sb);
        _sb.Append(']');
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter Field(string name, string value)
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        WriteString(value);
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter FieldRaw(string name, Action<CanonicalJsonWriter> value)
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        var inner = new CanonicalJsonWriter();
        value(inner);
        _sb.Append(inner._sb);
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter FieldNullableString(string name, string? value)
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        if (value is null)
        {
            _sb.Append("null");
        }
        else
        {
            WriteString(value);
        }
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter FieldLong(string name, long value)
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        _sb.Append(value.ToString(CultureInfo.InvariantCulture));
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter FieldNullableLong(string name, long? value)
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        _sb.Append(value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null");
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter FieldNullableInt(string name, int? value)
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        _sb.Append(value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null");
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter FieldBool(string name, bool value)
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        _sb.Append(value ? "true" : "false");
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter FieldStringArray(string name, IEnumerable<string> values)
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        _sb.Append('[');
        bool first = true;
        foreach (var v in values)
        {
            if (!first)
            {
                _sb.Append(',');
            }
            first = false;
            WriteString(v);
        }
        _sb.Append(']');
        _needsComma = true;
        return this;
    }

    internal CanonicalJsonWriter FieldNullableStringArray(string name, IEnumerable<string>? values)
    {
        if (values is null)
        {
            Comma();
            WriteString(name);
            _sb.Append(":null");
            _needsComma = true;
            return this;
        }
        return FieldStringArray(name, values);
    }

    internal CanonicalJsonWriter FieldNullableObj<T>(string name, T? value, Action<CanonicalJsonWriter, T> body) where T : class
    {
        Comma();
        WriteString(name);
        _sb.Append(':');
        if (value is null)
        {
            _sb.Append("null");
        }
        else
        {
            var inner = new CanonicalJsonWriter();
            body(inner, value);
            _sb.Append('{').Append(inner._sb).Append('}');
        }
        _needsComma = true;
        return this;
    }

    internal void WriteRawString(string value)
    {
        Comma();
        WriteString(value);
        _needsComma = true;
    }

    private void WriteString(string s)
    {
        _sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': _sb.Append("\\\""); break;
                case '\\': _sb.Append("\\\\"); break;
                case '\n': _sb.Append("\\n"); break;
                case '\r': _sb.Append("\\r"); break;
                case '\t': _sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        _sb.Append(c);
                    }
                    break;
            }
        }
        _sb.Append('"');
    }
}

internal static class Hashing
{
    internal static string Sha256Hex(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>ISO-8601 UTC, yyyy-MM-ddTHH:mm:ss.fffffffZ, per CONTRACT.md section 5.</summary>
    internal static string FormatWall(DateTimeOffset dt) =>
        dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
}
