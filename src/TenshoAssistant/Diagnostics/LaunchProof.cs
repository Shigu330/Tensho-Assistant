using System.Security.Cryptography;
using System.Text.Json;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.1126 (X-144, audit A13): the proof a release reads before it writes « Checked at launch ». After each self-check pass with a character
/// logged in, the plugin writes LaunchProof.json in its configuration folder: its version, the SHA-256 of the DLL the game loaded (read at
/// load, so a later build does not change it), the time and the pass's counts. The release script (scripts/LaunchProof.ps1) accepts it only
/// for the version released, with the same DLL as the one in latest.zip, no failure and a recent date; otherwise the release says « not
/// checked in game yet ». Numbers and a hash only: no character name, no path.
/// </summary>
public sealed record LaunchProof(int Format, string Version, string Dll, DateTimeOffset At, bool LoggedIn, string Reason, int Ok, int Seen, int NotRead, int Suspect, int Failed)
{
    public const int CurrentFormat = 1;
    public const string FileName = "LaunchProof.json";

    public static LaunchProof Make(string version, string dll, DateTimeOffset at, bool loggedIn, string reason, IReadOnlyCollection<SelfCheckLine> lines)
    {
        int Count(SelfCheckStatus status) => lines.Count(line => line.Status == status);
        return new(CurrentFormat, version, dll, at, loggedIn, reason, Count(SelfCheckStatus.Ok), Count(SelfCheckStatus.Seen), Count(SelfCheckStatus.NotRead),
            Count(SelfCheckStatus.Suspect), Count(SelfCheckStatus.Failed));
    }

    public string Serialize() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>The SHA-256 of a file in lower-case hexadecimal, or "" when it cannot be read (then no proof can match).</summary>
    public static string Hash(string? path)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return string.Empty;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return string.Empty; }
    }
}
