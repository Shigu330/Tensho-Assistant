// 0.992: the readers behind the « Long CompetitiveGate…Guarded » hitches named in developer mode, once each; the cut still decides first.
internal static class SlowReaders992Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Slow readers 992: timed only in developer mode, over 50 ms, named once; the cut first",() =>
        {
            var gate=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs"));
            Check(gate.Contains("void Guarded(IFramework running){if(!cut)Timed(handler,running);}"),"cut first");
            Check(gate.Contains("if(Developer?.Invoke()!=true){handler(running);return;}") && gate.Contains("if(spent<=50)return;") && gate.Contains("if(!slowNamed.Add(name))return;"),"developer, 50 ms, once");
        }),
    ];
}
