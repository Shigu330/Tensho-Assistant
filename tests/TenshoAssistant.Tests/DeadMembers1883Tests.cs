// X-816 (0.1883): the dead private members found by the user's analysis (IDE0051, IDE0052) and the unused parameters (IDE0060) are gone,
// and the Release build of the plugin and of the tests now refuses new ones: .editorconfig sets the three rules to error for these two
// projects, and both csproj run the code style analysis in Release (EnforceCodeStyleInBuild). The real proof is the build itself.
internal static class DeadMembers1883Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dead members 1883: .editorconfig sets IDE0051, IDE0052 and IDE0060 to error for the plugin and the tests", () =>
        {
            var config = Read(".editorconfig");
            var at = config.IndexOf("[{src/TenshoAssistant,tests/TenshoAssistant.Tests}/**.cs]", StringComparison.Ordinal);
            Check(at >= 0, "a section for the plugin's and the tests' sources");
            var section = config[at..];
            var next = section.IndexOf("\n[", 1, StringComparison.Ordinal);
            if (next > 0) section = section[..next];
            foreach (var rule in new[] { "IDE0051", "IDE0052", "IDE0060" })
                Check(section.Contains($"dotnet_diagnostic.{rule}.severity = error", StringComparison.Ordinal), rule + " is an error");
        }),
        ("Dead members 1883: the plugin and the tests run the code style analysis in their Release build", () =>
        {
            foreach (var project in new[] { "src/TenshoAssistant/TenshoAssistant.csproj", "tests/TenshoAssistant.Tests/TenshoAssistant.Tests.csproj" })
            {
                var text = Read(project);
                Check(text.Contains("<EnforceCodeStyleInBuild Condition=\"'$(Configuration)'=='Release'\">true</EnforceCodeStyleInBuild>", StringComparison.Ordinal), project + ": EnforceCodeStyleInBuild in Release");
                Check(text.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", StringComparison.Ordinal), project + ": warnings stay errors");
            }
        }),
        ("Dead members 1883: the chat map link watcher keeps neither the chat nor the client (its subscriptions use the parameters)", () =>
        {
            var watcher = Read("src/TenshoAssistant/Maps/ChatMapLinkWatcher.Dalamud.cs");
            Check(!watcher.Contains("private readonly IChatGui chat;", StringComparison.Ordinal) && !watcher.Contains("private readonly IClientState client;", StringComparison.Ordinal), "no field kept");
            Check(watcher.Contains("registrations.Register(()=>chat.ChatMessageUnhandled+=OnMessage,()=>chat.ChatMessageUnhandled-=OnMessage);", StringComparison.Ordinal)
                && watcher.Contains("registrations.Register(()=>client.Logout+=OnLogout,()=>client.Logout-=OnLogout);", StringComparison.Ordinal), "both subscriptions still made and released");
        }),
        ("Dead members 1883: the stores' Load no longer takes a time it never used (left over from the removed conversion, X-574)", () =>
        {
            Check(Read("src/TenshoAssistant/Collections/AchievementProgressData.cs").Contains("AchievementProgress>> Load()", StringComparison.Ordinal), "achievement progress");
            Check(Read("src/TenshoAssistant/FreeCompany/VoyageHistory.cs").Contains("public List<VoyageRecord> Load()", StringComparison.Ordinal), "voyage history");
            Check(Read("src/TenshoAssistant/Inventory/GilJournalStore.cs").Contains("public List<GilEntry> Load(ulong character)", StringComparison.Ordinal), "gil journal");
        }),
    ];
}
