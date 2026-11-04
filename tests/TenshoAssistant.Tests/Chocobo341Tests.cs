// 0.341 (user request with screenshot): the chocobo has its own page; the Character page is removed (the Gear page shows the same real
// values), the panel beside the game's Character window stays; the unlocked mark uses the icon font and the owned mark stays on its line.
internal static class Chocobo341Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chocobo page: own page in Progression, Character page gone, game-window panel kept, marks drawn from the icon font",() =>
        {
            var window = Read("MainWindow.cs");
            var pages=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Pages.cs"));Check(pages.Contains("new(Page.Chocobo,\"Progression\",PageLabel(\"Navigation.Chocobo\"),DrawChocoboPage,",StringComparison.Ordinal) && !window.Contains("Page.Character",StringComparison.Ordinal),"Chocobo page, no Character page");
            Check(!Read("MainWindow.Home.cs").Contains("Page.Character",StringComparison.Ordinal) && Read("MainWindow.Tools.cs").Contains("case \"Character Panel Refined\":page=Page.Gear;",StringComparison.Ordinal),"home tile and tool directory point to Gear");
            var character = Read("MainWindow.Character.cs");
            Check(!character.Contains("DrawCharacterStats",StringComparison.Ordinal) && character.Contains("internal void DrawCharacterPanel()",StringComparison.Ordinal),"page removed, panel kept");
            Check(!Read("MainWindow.Gear.cs").Contains("DrawChocoboBarding",StringComparison.Ordinal),"barding no longer at the bottom of Gear");
            var page = Read("MainWindow.ChocoboBarding.cs");
            Check(!page.Contains("\"✗\"",StringComparison.Ordinal) && page.Contains("FontAwesomeIcon.Times",StringComparison.Ordinal),"no « ✗ » glyph (missing from the font)");
            Check(page.Contains("ImGui.SameLine();DrawOwnedMark(item.Id);",StringComparison.Ordinal),"owned mark on the item's line");
            Check(page.Contains("CharacterUi.Offline",StringComparison.Ordinal),"says when no character is logged in");
        }),
    ];
}
