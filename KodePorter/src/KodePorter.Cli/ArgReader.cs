namespace KodePorter.Cli;

/// <summary>Thrown for CLI usage errors (missing/unknown arguments, bad verb). Exit code 1 (CONTRACT.md §9).</summary>
internal sealed class CliUsageException(string message) : Exception(message);

/// <summary>Thrown for CLI domain errors surfaced deliberately by verb logic. Exit code 2 (CONTRACT.md §9).</summary>
internal sealed class CliDomainException(string message) : Exception(message);

/// <summary>
/// Manual `--flag value` argument parsing (CONTRACT.md §9: "manual arg parsing, no packages").
/// Every flag takes exactly one value; unrecognized bare tokens are collected as positional
/// arguments (used for sub-verbs like `unit new`).
/// </summary>
internal sealed class ArgReader
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public List<string> Positional { get; } = [];

    public ArgReader(IReadOnlyList<string> args)
    {
        for (int i = 0; i < args.Count; i++)
        {
            string a = args[i];
            if (a.StartsWith("--", StringComparison.Ordinal))
            {
                string key = a[2..];
                if (key.Length == 0)
                    throw new CliUsageException("Empty '--' flag.");
                if (i + 1 >= args.Count)
                    throw new CliUsageException($"Missing value for '--{key}'.");
                _values[key] = args[++i];
            }
            else
            {
                Positional.Add(a);
            }
        }
    }

    public string Require(string name) =>
        _values.TryGetValue(name, out var v) ? v : throw new CliUsageException($"Missing required --{name}.");

    public string? Optional(string name) => _values.GetValueOrDefault(name);

    public string OptionalOr(string name, string fallback) => _values.GetValueOrDefault(name, fallback);

    public static IReadOnlyList<string> SplitCsv(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
