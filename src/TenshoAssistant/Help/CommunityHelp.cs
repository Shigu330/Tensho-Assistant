using System.Net.Http;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Community;

namespace TenshoAssistant.Help;

/// <summary>A video or a written guide proposed by the community for one page (ONBOARD-001, lot 7, G6, G7).</summary>
public sealed record CommunityGuide(string Page, string Language, string Title, string? Author, string Address, string? ForVersion);
public sealed record CommunityHelpState(bool Loading, IReadOnlyList<CommunityGuide>? Guides, string? Problem);

/// <summary>
/// community-help/index.json in this repository, reviewed in pull requests. Read on the player's click only, on <c>main</c>, bounded in size. Only data: a link
/// goes to the player's browser after a confirmation showing its address, and only to the hosts listed here. Adding a host is a change of the plugin, reviewed.
/// </summary>
public sealed class CommunityHelpIndex : IDisposable
{
    public const string Address = "https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/community-help/index.json";
    public const int MaxBytes = 256 * 1024, MaxGuides = 1000, MaxTitle = 120, MaxAuthor = 60;
    /// <summary>G6: YouTube first, and the project's own repository for written guides.</summary>
    public static readonly IReadOnlySet<string> Hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "www.youtube.com", "youtube.com", "youtu.be", "github.com" };
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly object gate = new();
    private CommunityHelpState state = new(false, null, null);
    private CancellationTokenSource? running;
    private bool disposed;
    public CommunityHelpState State { get { lock (gate) return state; } }

    public CommunityHelpIndex(HttpClient? client = null)
    {
        ownsClient = client is null;
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    /// <summary>An address the plugin may open: https, a listed host; for GitHub, this repository only.</summary>
    public static bool Allowed(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length > 0 || !Hosts.Contains(uri.Host)) return false;
        return !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath.StartsWith("/Shigu330/Tensho-Assistant/", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Clean(string? text, int maximum)
    {
        if (text is null) return null;
        var line = new string(text.Where(letter => !char.IsControl(letter)).ToArray()).Trim();
        return line.Length is 0 || line.Length > maximum ? null : line;
    }

    /// <summary>The list, or null with one reason when any row breaks a rule: a broken index is refused whole, never partly shown.</summary>
    public static (IReadOnlyList<CommunityGuide>? Guides, string? Problem) Parse(string json, IReadOnlySet<string> pages)
    {
        const string broken = "Broken";
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out var version)) return (null, broken);
            if (version != 1) return (null, "Newer");
            if (!root.TryGetProperty("guides", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > MaxGuides) return (null, broken);
            var guides = new List<CommunityGuide>();
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object) return (null, broken);
                string? Text(string name) => row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
                var page = Text("page"); var language = Text("language"); var address = Text("address");
                var title = Clean(Text("title"), MaxTitle);
                if (page is null || !pages.Contains(page) || language is not { Length: 2 } || address is null || !Allowed(address) || title is null) return (null, broken);
                guides.Add(new(page, language, title, Clean(Text("author"), MaxAuthor), address, Clean(Text("forVersion"), 20)));
            }
            return (Array.AsReadOnly(guides.ToArray()), null);
        }
        catch (JsonException) { return (null, broken); }
    }

    /// <summary>A page's guides in the player's language first, then English; other languages are not shown.</summary>
    public static IReadOnlyList<CommunityGuide> For(IEnumerable<CommunityGuide> guides, string page, string language)
        => Array.AsReadOnly(guides.Where(guide => guide.Page == page && (guide.Language == language || guide.Language == "en")).OrderByDescending(guide => guide.Language == language).ToArray());

    /// <summary>Read once in a session, on the player's click; again only if the last try failed.</summary>
    public async Task LoadAsync(IReadOnlySet<string> pages)
    {
        CancellationToken token;
        lock (gate)
        {
            if (disposed || state.Loading || state.Guides is not null) return;
            running?.Dispose();
            running = new();
            token = running.Token;
            state = new(true, null, null);
        }
        (IReadOnlyList<CommunityGuide>?, string?) result;
        try
        {
            // X-76 (step 2): the shared bounded reader (size limit, Tensho's User-Agent, strict UTF-8); a refused status is an HttpRequestException.
            result = Parse(await BoundedHttp.ReadAsync(client, new Uri(Address), MaxBytes, token).ConfigureAwait(false), pages);
        }
        catch (OperationCanceledException) { result = (null, "Unavailable"); }
        catch (HttpRequestException) { result = (null, "Unavailable"); }
        catch (Exception error) when (error is InvalidDataException or DecoderFallbackException) { result = (null, "Broken"); }
        lock (gate) if (!disposed) state = new(false, result.Item1, result.Item2);
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            running?.Cancel();
            running?.Dispose();
            running = null;
        }
        if (ownsClient) client.Dispose();
    }
}
