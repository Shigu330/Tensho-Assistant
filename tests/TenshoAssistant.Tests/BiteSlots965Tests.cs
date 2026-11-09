// X-98 (0.965): an unknown bite strength logs the timeline slots in developer mode (to find the slot used at sea).
internal static class BiteSlots965Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Bite slots 965: logged when the strength is unknown, developer mode only",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(biteStrength==0 && debug())") && watcher.Contains("Discovery: bite strength unknown; timeline slots {0}."),"logged");
        }),
    ];
}
