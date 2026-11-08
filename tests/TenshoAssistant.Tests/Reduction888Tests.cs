// 0.888 (X-73.18): aetherial reduction from AgentPurify, and the capture never writes a message naming a player.
using TenshoAssistant.Discoveries;
internal static class Reduction888Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Reduction: collectable and HQ offsets removed, read while the result shows, once per result; messages naming a player never written",() =>
        {
            Check(ItemGains.BaseItem(543_934)==43_934 && ItemGains.BaseItem(1_005_000)==5_000 && ItemGains.BaseItem(8)==8,"offsets");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(gui.GetAddonByName(\"PurifyResult\") is not { IsNull:false,IsVisible:true }){purifySeen=\"\";return;}")
                && watcher.Contains("if(results.Count==0 || signature==purifySeen)return;"),"result window, once");
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("if(message.Message.Payloads.Any(payload=>payload is Dalamud.Game.Text.SeStringHandling.Payloads.PlayerPayload))return;"),"privacy");
        }),
    ];
}
