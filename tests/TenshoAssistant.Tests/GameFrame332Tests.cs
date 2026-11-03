using TenshoAssistant;

// 0.332 (step e, ULD before D17): the game's window frame is opt-in, uses only textures proved by tools/UldProbe, and draws nothing
// when one is missing (R-14). Drawing itself is Dalamud code: pinned in the source.
internal static class GameFrame332Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game frame: off by default, reset with the settings, only proved textures, nothing drawn when one is missing",() =>
        {
            Check(!new Configuration().GameFrames && ConfigurationReset.Reset.Contains("GameFrames"),"off by default, reset with the settings");
            var frame = File.ReadAllText(Path.Combine(Source,"UI","Components","GameFrame.cs"));
            var evidence = File.ReadAllText(Path.GetFullPath("docs/evidence/0.332-uld-window-frame.md"));
            foreach (var texture in new[] { "WindowA_BgNormal_Corner_hr1.tex","WindowA_BgNormal_H_hr1.tex","WindowA_BgNormal_V_hr1.tex","WindowA_BgNormal_HV_hr1.tex" })
                Check(frame.Contains("ui/uld/" + texture,StringComparison.Ordinal) && evidence.Contains(texture,StringComparison.Ordinal),"texture proved by the probe: " + texture);
            Check(frame.Contains("if(!configuration.GameFrames)return;",StringComparison.Ordinal) && frame.Contains("|| middle is null)return;",StringComparison.Ordinal),"opt-in, and nothing drawn without every texture");
            Check(frame.Contains("draw.PushClipRect(min,max,false);",StringComparison.Ordinal) && frame.Contains("finally{draw.PopClipRect();}",StringComparison.Ordinal),"clip rectangle balanced");
        }),
    ];
}
