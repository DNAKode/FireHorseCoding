namespace GovernanceLedger;

/// <summary>Thrown for CLI usage errors (missing/unknown arguments, bad verb). Exit code 1.</summary>
internal sealed class CliUsageException(string message) : Exception(message);

/// <summary>Thrown for CLI domain errors surfaced deliberately by verb logic. Exit code 2.</summary>
internal sealed class CliDomainException(string message) : Exception(message);

/// <summary>
/// Manual `--flag value` argument parsing (mirrors KodePorter.Cli's ArgReader; deliberately not
/// shared via a project reference — GovernanceLedger references Gneiss.Cell ONLY, per
/// CONTRACT-M15.md section 7). Every flag takes exactly one value; unrecognized bare tokens are
/// collected as positional arguments.
/// </summary>
internal sealed class ArgReader
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly HashSet<string> _flags = new(StringComparer.Ordinal);

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
                if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    // Boolean switch: present with no value (e.g. --proposed).
                    _flags.Add(key);
                    continue;
                }
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

    public bool Switch(string name) => _flags.Contains(name) || _values.ContainsKey(name);
}
