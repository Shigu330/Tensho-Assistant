using TenshoAssistant.Duties;

// X-90 (0.995): the player's note and arena drawing per duty, shared by a code; hidden in combat, never drawn over the game.
internal static class DutyNotes995Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Duty notes 995: share code round trip, bounds kept, wrong codes refused",() =>
        {
            var note=new DutyNote{Text="Tanks north",Shape=ArenaShape.Square,Tokens=[new(){Label="T1",X=0,Y=-0.9f},new(){Label="LONGLABEL",X=5,Y=float.NaN}]};
            var code=DutyArena.Share(42,note);
            Check(code.StartsWith("TENSHO-ARENA1:",StringComparison.Ordinal),"prefix");
            Check(DutyArena.Parse(code) is { Duty:42 } back && back.Note.Text=="Tanks north" && back.Note.Shape==ArenaShape.Square && back.Note.Tokens.Count==2
                && back.Note.Tokens[1].Label=="LONG" && back.Note.Tokens[1].X==1 && back.Note.Tokens[1].Y==0,"round trip, clamped");
            Check(DutyArena.Parse("hello")==null && DutyArena.Parse("TENSHO-ARENA1:@@@")==null && DutyArena.Parse(null)==null,"refused");
        }),
        ("Duty notes 995: hidden in combat, kept with the player's data",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Duties.cs"));
            Check(page.Contains("private void DrawDutyNotes(DutyDefinition duty)") && page.Contains("if(InCombatNow?.Invoke()==true){EmptyState(\"DutyGuide.InCombat\");return;}"),"combat");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Configuration.cs")).Contains("\"DutyNotes\""),"kept");
        }),
    ];
}
