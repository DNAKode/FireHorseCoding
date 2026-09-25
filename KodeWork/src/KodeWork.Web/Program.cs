using System.Text.Json;
using System.Text.Json.Serialization;
using KodeWork.Core;
using Microsoft.AspNetCore.HttpOverrides;

string home = Environment.GetEnvironmentVariable("KODEWORK_HOME")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "var", "kodework");
string? data = Environment.GetEnvironmentVariable("KODEWORK_DATA");
string actorDefault = Environment.GetEnvironmentVariable("KODEWORK_ACTOR") ?? "operator";

Directory.CreateDirectory(home);
var board = KodeWorkBoard.Initialize(home, data);
board.Project();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("KODEWORK_URLS") ?? "http://127.0.0.1:18790");
var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});
app.UsePathBase("/kodework");

var json = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

app.MapGet("/", () => Results.Content(IndexHtml.Value, "text/html; charset=utf-8"));
app.MapGet("/index.html", () => Results.Content(IndexHtml.Value, "text/html; charset=utf-8"));

app.MapGet("/api/tree", () =>
{
    var view = board.Current();
    return Results.Json(new
    {
        label = view.Label,
        roots = view.Roots,
        orphans = view.Orphans,
        notes = view.Notes,
    }, json);
});

app.MapPost("/api/items", (AddItemRequest req, HttpRequest http) =>
{
    string actor = Actor(http, actorDefault);
    var item = board.Add(actor, "web add", req);
    board.ProjectAndCommit($"add {item.Title}");
    return Results.Json(item, json);
});

app.MapPatch("/api/items/{id}", (string id, SetItemRequest req, HttpRequest http) =>
{
    string actor = Actor(http, actorDefault);
    var item = board.Set(actor, "web set", id, req);
    board.ProjectAndCommit($"set {item.Title}");
    return Results.Json(item, json);
});

app.MapGet("/api/items/{id}/why", (string id) =>
{
    var item = board.Get(id);
    if (item?.TitleAid is null)
    {
        return Results.NotFound();
    }
    var expl = board.Why(item.TitleAid);
    return Results.Json(new { aid = expl.Aid, status = expl.Status, defeatedBy = expl.DefeatedBy, decisions = expl.Decisions, text = FormatWhy(expl, 0) }, json);
});

app.MapPost("/api/notes", (NoteBody body, HttpRequest http) =>
{
    string actor = Actor(http, actorDefault);
    string id = board.Note(actor, body.Text);
    return Results.Json(new { id });
});

app.Lifetime.ApplicationStopping.Register(board.Dispose);
app.Run();

static string Actor(HttpRequest http, string fallback) =>
    http.Headers.TryGetValue("X-KodeWork-Actor", out var v) && !string.IsNullOrWhiteSpace(v)
        ? v.ToString()
        : fallback;

static string FormatWhy(Gneiss.Cell.Explanation e, int depth) =>
    new string(' ', depth * 2) + e.Aid[..Math.Min(12, e.Aid.Length)] + " " + e.Status +
    (e.DefeatedBy is null ? "" : " by " + e.DefeatedBy) + "\n" +
    string.Concat(e.Inputs.Select(i => FormatWhy(i, depth + 1)));

internal sealed record NoteBody(string Text);

internal static class IndexHtml
{
    internal static readonly string Value = Read();

    private static string Read()
    {
        var asm = typeof(IndexHtml).Assembly;
        using var s = asm.GetManifestResourceStream("KodeWork.Web.wwwroot.index.html")
            ?? throw new InvalidOperationException("index.html resource missing");
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }
}
