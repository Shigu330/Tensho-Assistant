using System.IO.Compression;
using System.Text.Json;

// 0.1330 (X-344): the installed plugin showed Dalamud's « ? » instead of its icon. Dalamud reads an installed plugin's icon from the IconUrl of
// the manifest that comes inside latest.zip (copied into installedPlugins/<name>/<version>/ on install); repo.json's IconUrl is used by the
// list of available plugins only, and images/icon.png beside the DLL by a dev plugin only. The source manifest now carries the IconUrl,
// Verify.ps1 refuses a package without it, Release.ps1 refuses a repo.json whose icon differs, and the CI checks it
// (docs/evidence/x344-plugin-icon.md).
internal static class PluginIcon1330Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
    private const string IconUrl = "https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/src/TenshoAssistant/images/icon.png";

    private static string? IconOf(string manifestText)
    {
        using var manifest = JsonDocument.Parse(manifestText);
        return manifest.RootElement.TryGetProperty("IconUrl", out var icon) ? icon.GetString() : null;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Plugin icon 1330: the source manifest names the icon served from main, and the icon is a PNG of the repository", () =>
        {
            Check(IconOf(Read("src/TenshoAssistant/TenshoAssistant.json")) == IconUrl, "the source manifest's IconUrl");
            var bytes = File.ReadAllBytes(Path.GetFullPath("src/TenshoAssistant/images/icon.png"));
            Check(bytes.Length > 24 && bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G', "images/icon.png is a PNG");
            var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            var height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            Check(width == height && width >= 64 && width <= 512, $"a square icon of 64 to 512 pixels ({width}x{height})");
        }),
        ("Plugin icon 1330: the built package's manifest, the one Dalamud installs, carries the IconUrl", () =>
        {
            // Verify.ps1 runs the tests after the build: the package is there and newer than the source manifest. A run without that build
            // (check.sh builds elsewhere, the CI tests the sources) checks the sources only; Verify.ps1 checks the package itself.
            var output = Path.GetFullPath("src/TenshoAssistant/bin/x64/Release/TenshoAssistant/latest.zip");
            if (!File.Exists(output) || File.GetLastWriteTimeUtc(output) < File.GetLastWriteTimeUtc(Path.GetFullPath("src/TenshoAssistant/TenshoAssistant.json"))) return;
            using var zip = ZipFile.OpenRead(output);
            var entry = zip.GetEntry("TenshoAssistant.json") ?? throw new Exception("no manifest in latest.zip");
            using var reader = new StreamReader(entry.Open());
            Check(IconOf(reader.ReadToEnd()) == IconUrl, "latest.zip's manifest has the IconUrl");
        }),
        ("Plugin icon 1330: Verify, the release and the CI refuse a package without the icon", () =>
        {
            var verify = Read("scripts/Verify.ps1");
            var icon = verify.IndexOf("if ($taskManifest.IconUrl -ne $taskIconUrl) { throw", StringComparison.Ordinal);
            Check(icon > 0 && verify.Contains($"$taskIconUrl = '{IconUrl}'", StringComparison.Ordinal), "Verify.ps1 checks the packaged manifest's IconUrl");
            Check(icon < verify.IndexOf("$taskZip = [IO.Compression.ZipFile]::OpenRead(", StringComparison.Ordinal), "before the package is accepted");
            Check(verify.Contains("Plugin icon missing: src/TenshoAssistant/images/icon.png must be a PNG file", StringComparison.Ordinal), "and that the icon file exists");
            Check(Read("scripts/Release.ps1").Contains("if ($manifest.IconUrl -ne \"$imagesUrl/icon.png\") { Stop-Release", StringComparison.Ordinal), "Release.ps1: repo.json and the package name the same icon");
            Check(Read(".github/workflows/build.yml").Contains("if (-not $manifest.IconUrl) { throw", StringComparison.Ordinal), "the CI build checks it too");
        }),
    ];
}
