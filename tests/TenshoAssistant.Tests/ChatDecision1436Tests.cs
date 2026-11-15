// 0.1436 (X-439, D-166): the user's « Fenêtre en plus » of 2026-11-15 is DEC-053, in both halves of DECISIONS.md; DEC-030 keeps its
// text and says it is reopened in part by DEC-053.
internal static class ChatDecision1436Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Block(string text,string marker,string id)
    {
        var start = text.IndexOf("\n" + marker + ": " + id + "\n",StringComparison.Ordinal);
        Check(start >= 0,marker + ": " + id + " found");
        var end = text.IndexOf("\n\n",start + 1,StringComparison.Ordinal);
        return end < 0 ? text[start..] : text[start..end];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("DECISIONS: DEC-053 (the extra chat window) in both halves, with the user's words and both options",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md")).Replace("\r\n","\n");
            var en = Block(text,"ID","DEC-053");
            var fr = Block(text,"DEC-ID","DEC-053");
            foreach(var (half,block) in new[]{("English",en),("French",fr)})
            {
                Check(block.Contains("« Fenêtre en plus »"),half + ": the user's words");
                Check(block.Contains("(a) ") && block.Contains("(b) "),half + ": options a and b");
                Check(block.Contains("DEC-030") && block.Contains("DEC-038") && block.Contains("X-403"),half + ": DEC-030, DEC-038 and X-403 named");
            }
            Check(en.Contains("no automatic sending") && en.Contains("Typing and replacing the game's chat are decided later"),"English: red lines and typing left for later");
            Check(fr.Contains("aucun envoi automatique") && fr.Contains("La saisie et le remplacement du chat du jeu se décident plus tard"),"French: red lines and typing left for later");
            Check(text.IndexOf("\nID: DEC-053\n",StringComparison.Ordinal) < text.IndexOf("\n---\n",StringComparison.Ordinal),"DEC-053 English entry before the French half");
            Check(!AgentPaths1329Tests.Cites(text),"no path to the agent's files (R22)");
        }),
        ("DECISIONS: DEC-030 keeps its decision and says it is reopened in part by DEC-053",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/DECISIONS.md")).Replace("\r\n","\n");
            var en = Block(text,"ID","DEC-030");
            var fr = Block(text,"DEC-ID","DEC-030");
            Check(en.Contains("Decision: Customisable chat abandoned and removed") && fr.Contains("Decision: Chat personnalisable abandonné et retiré"),"DEC-030 decision unchanged");
            Check(en.Contains("\nReopened: in part by DEC-053 (2026-11-15)"),"DEC-030 reopened line (English)");
            Check(fr.Contains("\nRouverte : en partie par DEC-053 (2026-11-15)"),"DEC-030 reopened line (French)");
        }),
    ];
}
