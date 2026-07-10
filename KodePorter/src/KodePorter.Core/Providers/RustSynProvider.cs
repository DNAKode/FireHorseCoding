using System.Text.Json;
using KodePorter.Core.Model;
using KodePorter.Core.Store;

namespace KodePorter.Core.Providers;

/// <summary>
/// Imports a provider dump JSON file (fixtures/slice-zero/CONTRACT.md §6) produced by
/// `tools/rust-map-dump` into entity rows. Does NOT run cargo or syn itself (CONTRACT.md §3).
/// </summary>
public sealed class RustSynProvider
{
    private const string RequiredProviderPrefix = "rust-syn@";

    /// <summary>Reads the dump at <paramref name="dumpJsonPath"/> and writes its entities under <paramref name="basis"/>.</summary>
    public ImportResult Import(MapStore store, Basis basis, string dumpJsonPath)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(basis);
        ArgumentException.ThrowIfNullOrEmpty(dumpJsonPath);

        if (!File.Exists(dumpJsonPath))
            throw new FileNotFoundException($"Rust dump JSON not found at '{dumpJsonPath}'.", dumpJsonPath);

        string json = File.ReadAllText(dumpJsonPath);
        ProviderDump dump;
        try
        {
            dump = JsonSerializer.Deserialize<ProviderDump>(json)
                ?? throw new InvalidDataException($"Rust dump JSON at '{dumpJsonPath}' deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Rust dump JSON at '{dumpJsonPath}' is not valid JSON: {ex.Message}", ex);
        }

        if (string.IsNullOrEmpty(dump.Provider) || !dump.Provider.StartsWith(RequiredProviderPrefix, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Rust dump JSON at '{dumpJsonPath}' has provider '{dump.Provider}'; expected it to start with '{RequiredProviderPrefix}'.");
        }

        for (int i = 0; i < dump.Entities.Count; i++)
            ValidateEntity(dump.Entities[i], i, dumpJsonPath);

        var deduplicated = EntityResolution.SortAndDeduplicate(dump.Entities);
        var entities = EntityResolution.ToEntities(deduplicated, basis.Side, basis.Id);

        store.InsertEntities(basis.Id, entities);

        return new ImportResult(entities.Count, ErrorDiagnosticCount: 0);
    }

    private static void ValidateEntity(DumpEntity e, int index, string dumpJsonPath)
    {
        if (string.IsNullOrEmpty(e.Kind))
            throw new InvalidDataException($"Rust dump JSON at '{dumpJsonPath}': entities[{index}] is missing 'kind'.");
        if (string.IsNullOrEmpty(e.SymbolPath))
            throw new InvalidDataException($"Rust dump JSON at '{dumpJsonPath}': entities[{index}] is missing 'symbolPath'.");
        if (string.IsNullOrEmpty(e.File))
            throw new InvalidDataException($"Rust dump JSON at '{dumpJsonPath}': entities[{index}] is missing 'file'.");
        if (string.IsNullOrEmpty(e.ContentHash))
            throw new InvalidDataException($"Rust dump JSON at '{dumpJsonPath}': entities[{index}] is missing 'contentHash'.");
    }
}
