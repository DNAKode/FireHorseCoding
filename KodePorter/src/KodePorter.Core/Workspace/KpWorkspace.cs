using System.Text;
using System.Text.Json;
using KodePorter.Core.Store;

namespace KodePorter.Core.Workspace;

/// <summary>
/// A port workspace directory (CONTRACT.md §1): the pieces owned by this increment are
/// kp.json (the project descriptor) and kpmap.db (the map store). Other workspace members
/// (gneiss.db, .kodeporter/, checkouts/, runs/, atlas/) belong to later increments.
/// </summary>
public sealed class KpWorkspace : IDisposable
{
    private static readonly JsonSerializerOptions KpJsonOptions = new() { WriteIndented = true };

    public string WorkspaceDir { get; }
    public ProjectDescriptor Project { get; }
    public MapStore Map { get; }

    private KpWorkspace(string workspaceDir, ProjectDescriptor project, MapStore map)
    {
        WorkspaceDir = workspaceDir;
        Project = project;
        Map = map;
    }

    public static string KpJsonPath(string workspaceDir) => Path.Combine(workspaceDir, "kp.json");

    public static string KpMapDbPath(string workspaceDir) => Path.Combine(workspaceDir, "kpmap.db");

    /// <summary>
    /// Creates the workspace directory and kp.json if they do not already exist, then opens
    /// (creating if necessary) kpmap.db. If kp.json already exists, the <paramref name="project"/>
    /// argument is ignored and the recorded descriptor is loaded instead.
    /// </summary>
    public static KpWorkspace Initialize(string workspaceDir, ProjectDescriptor project)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspaceDir);
        ArgumentNullException.ThrowIfNull(project);

        Directory.CreateDirectory(workspaceDir);

        string kpJsonPath = KpJsonPath(workspaceDir);
        ProjectDescriptor resolvedProject;
        if (File.Exists(kpJsonPath))
        {
            resolvedProject = LoadProjectDescriptor(kpJsonPath);
        }
        else
        {
            WriteProjectDescriptor(kpJsonPath, project);
            resolvedProject = project;
        }

        var map = new MapStore(KpMapDbPath(workspaceDir));
        return new KpWorkspace(workspaceDir, resolvedProject, map);
    }

    /// <summary>Opens an existing workspace. Throws if kp.json is missing.</summary>
    public static KpWorkspace Open(string workspaceDir)
    {
        ArgumentException.ThrowIfNullOrEmpty(workspaceDir);

        string kpJsonPath = KpJsonPath(workspaceDir);
        if (!File.Exists(kpJsonPath))
            throw new FileNotFoundException($"No kp.json found in workspace '{workspaceDir}'; run initialize first.", kpJsonPath);

        var project = LoadProjectDescriptor(kpJsonPath);
        var map = new MapStore(KpMapDbPath(workspaceDir));
        return new KpWorkspace(workspaceDir, project, map);
    }

    private static ProjectDescriptor LoadProjectDescriptor(string kpJsonPath)
    {
        string json = File.ReadAllText(kpJsonPath);
        return JsonSerializer.Deserialize<ProjectDescriptor>(json)
            ?? throw new InvalidDataException($"kp.json at '{kpJsonPath}' deserialized to null.");
    }

    private static void WriteProjectDescriptor(string kpJsonPath, ProjectDescriptor project)
    {
        string json = JsonSerializer.Serialize(project, KpJsonOptions);
        File.WriteAllText(kpJsonPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public void Dispose()
    {
        Map.Dispose();
    }
}
