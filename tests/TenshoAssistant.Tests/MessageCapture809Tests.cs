// 0.809 (X-43, step 3): the game's messages captured, bounded, without player names, before reading drops, spells and catches.
internal static class MessageCapture809Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Message capture: the game's own messages, five per kind, eighty kinds, player names masked, cut in PvP",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("if(message.Sender.TextValue.Trim().Length!=0)return;"),"the game only");
            Check(capture.Contains("if(seen>=5 || messageKinds.Count>=80 && seen==0)return;") && capture.Contains("Line(\"messages\",400,"),"bounded");
            Check(capture.Contains("case Dalamud.Game.Text.SeStringHandling.Payloads.PlayerPayload:text.Append(\"<player>\");break;"),"names masked");
            Check(capture.IndexOf("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;")<capture.IndexOf("var kind=(int)message.LogKind;"),"cut first");
        }),
    ];
}
