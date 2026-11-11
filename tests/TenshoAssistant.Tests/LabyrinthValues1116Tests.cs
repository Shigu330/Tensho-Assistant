// 0.1116 (X-135 first step, R06): the board's result is captured before it is read: the labyrinth's own windows (XBM...) have their numbers
// written to the log in developer mode, in the labyrinth only, on change and bounded; never a text (a result window may name the character).
internal static class LabyrinthValues1116Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Labyrinth values 1116: numbers of the XBM windows only, developer mode and labyrinth only, on change, bounded, cleared on leaving",() =>
        {
            var gate=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs")).Replace("\r\n","\n");
            var start=gate.IndexOf("private unsafe void LabyrinthValues(",StringComparison.Ordinal);
            Check(start>0,"the capture exists");
            var body=gate[start..gate.IndexOf("public void Dispose()",start,StringComparison.Ordinal)];
            Check(body.Contains("AtkValueType.Int=>") && body.Contains("AtkValueType.UInt=>") && body.Contains("AtkValueType.Bool=>") && body.Contains("_=>null,"),"numbers only");
            Check(!body.Contains(".String") && !body.Contains("IsText") && !body.Contains("NodeText"),"never a text");
            Check(body.Contains("if(labyrinthValues.GetValueOrDefault(name)==line)return;") && body.Contains("if(written>=LabyrinthValueLines)return;"),"on change, bounded");
            Check(gate.Contains("if(name.StartsWith(\"XBM\",StringComparison.Ordinal))LabyrinthValues(unit,name);"),"the labyrinth's own windows");
            Check(gate.Contains("if(CompetitiveGate.Developer?.Invoke()==true && CompetitiveZones.IsLabyrinth((uint)client.TerritoryType))\n            try{ReadLabyrinth();}"),"developer mode, labyrinth only");
            Check(gate.Contains("else if(labyrinthValueLines.Count>0){labyrinthValueLines.Clear();labyrinthValues.Clear();}"),"a new visit writes again");
        }),
    ];
}
