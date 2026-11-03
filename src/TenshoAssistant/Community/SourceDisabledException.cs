namespace TenshoAssistant.Community;

/// <summary>
/// A community request refused because the player turned its source off (Settings › Advanced, 0.324). It never reaches the network;
/// copies already kept on disk stay readable. The page shows a translated « source turned off » line rather than an error.
/// </summary>
public sealed class SourceDisabledException(string source) : InvalidOperationException(source + Suffix)
{
    /// <summary>End of every « source turned off » message, the one the pages recognise.</summary>
    public const string Suffix = " is turned off (Settings › Advanced).";
    public string SourceName { get; } = source;
}
