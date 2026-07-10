// HeadScan.Cases: the C# case-runner for the Slice Zero JSONL harness
// protocol (CONTRACT.md §1.3). Reads one JSON case object per line from
// stdin: {"name":"case-id","inputB64":"<base64 of raw input bytes>"} and
// writes one JSON result line per case, in input order, to stdout:
// {"name":"case-id","result":<canonical result JSON>}
//
// The canonical result JSON (CONTRACT.md §1.2) is built by hand, field
// by field, mirroring the Rust source's `to_canonical_json` /
// `doc_to_json` / `push_json_string` (fixtures/slice-zero/rust/src/lib.rs),
// so the output is byte-exact regardless of any JSON library's
// serialization choices.

using System.Text;
using System.Text.Json;
using HeadScan;

using Stream stdinStream = Console.OpenStandardInput();
using var stdin = new StreamReader(stdinStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

using Stream stdoutStream = Console.OpenStandardOutput();
using var stdout = new StreamWriter(stdoutStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
{
    NewLine = "\n",
    AutoFlush = false,
};

string? line;
while ((line = stdin.ReadLine()) is not null)
{
    if (line.Length == 0)
    {
        continue;
    }

    using JsonDocument caseDoc = JsonDocument.Parse(line);
    JsonElement root = caseDoc.RootElement;
    string name = root.GetProperty("name").GetString() ?? "";
    string inputB64 = root.GetProperty("inputB64").GetString() ?? "";

    byte[] inputBytes = Convert.FromBase64String(inputB64);
    string input = Encoding.UTF8.GetString(inputBytes);

    ParseResult result = HeaderParser.Parse(input);

    var sb = new StringBuilder();
    sb.Append("{\"name\":");
    AppendJsonString(sb, name);
    sb.Append(",\"result\":");
    AppendCanonicalResult(sb, result);
    sb.Append('}');

    stdout.Write(sb.ToString());
    stdout.Write('\n');
}

stdout.Flush();

return 0;

// ---------------------------------------------------------------------
// Canonical JSON (CONTRACT.md §1.2). Built by hand, field by field, so
// the output is byte-exact regardless of any JSON library's map/struct
// field ordering behavior.
// ---------------------------------------------------------------------

static void AppendCanonicalResult(StringBuilder sb, ParseResult result)
{
    if (result.IsOk)
    {
        AppendDocJson(sb, result.Doc);
    }
    else
    {
        AppendErrorJson(sb, result.Error);
    }
}

static void AppendErrorJson(StringBuilder sb, ParseError error)
{
    sb.Append("{\"error\":{\"code\":\"");
    sb.Append(error.Code.AsStr());
    sb.Append("\",\"line\":");
    sb.Append(error.Line);
    sb.Append("}}");
}

static void AppendDocJson(StringBuilder sb, HeaderDoc doc)
{
    sb.Append("{\"fields\":[");
    for (int i = 0; i < doc.Fields.Count; i++)
    {
        if (i > 0)
        {
            sb.Append(',');
        }

        Field field = doc.Fields[i];
        sb.Append("{\"key\":");
        AppendJsonString(sb, field.Key);
        sb.Append(",\"kind\":\"");
        sb.Append(field.Value.Kind);
        sb.Append("\",\"line\":");
        sb.Append(field.Line);

        switch (field.Value)
        {
            case FieldValue.Text t:
                sb.Append(",\"value\":");
                AppendJsonString(sb, t.Value);
                break;
            case FieldValue.Count c:
                sb.Append(",\"value\":");
                sb.Append(c.Value);
                break;
            case FieldValue.Ratio r:
                sb.Append(",\"valueNanos\":");
                sb.Append(r.ValueNanos);
                break;
        }

        sb.Append('}');
    }

    sb.Append("],\"lineEnding\":\"");
    sb.Append(doc.LineEnding.AsStr());
    sb.Append("\",\"warnings\":{\"duplicates\":");
    sb.Append(doc.Duplicates);
    sb.Append("}}");
}

/// <summary>Render <paramref name="s"/> as a JSON string literal (with quotes).</summary>
static void AppendJsonString(StringBuilder sb, string s)
{
    sb.Append('"');
    foreach (char c in s)
    {
        switch (c)
        {
            case '"':
                sb.Append("\\\"");
                break;
            case '\\':
                sb.Append("\\\\");
                break;
            case '\n':
                sb.Append("\\n");
                break;
            case '\r':
                sb.Append("\\r");
                break;
            case '\t':
                sb.Append("\\t");
                break;
            default:
                if (c < 0x20)
                {
                    sb.Append("\\u");
                    sb.Append(((int)c).ToString("x4"));
                }
                else
                {
                    sb.Append(c);
                }

                break;
        }
    }

    sb.Append('"');
}
