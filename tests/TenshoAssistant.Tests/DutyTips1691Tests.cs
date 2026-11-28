using TenshoAssistant.Duties;

// 0.1691 (X-684, tenth part, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one duty tooltip (DrawDutyTooltip,
// facts in Duties/DutyTips): icon, name, kind and level, zone, progress, unlocking quest, ids in developer mode, then the click; the Duties
// page (before: the ids only, in developer mode), the deep dungeons' floors and a monster's duty go through it; built once while hovered.
internal static class DutyTips1691Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Duty tips 1691: the facts: blanks left out, the name trimmed; the ids as the page wrote them, for developer mode", () =>
        {
            var facts = DutyTips.Build(" Sastasha ", 61801, "Dungeons · level 15", " ", null, "Unlocked by: It's Probably Pirates · Done", "");
            Check(facts.Name == "Sastasha" && facts.Icon == 61801 && facts.Kind == "Dungeons · level 15" && facts.Zone is null && facts.Status is null
                  && facts.Unlock == "Unlocked by: It's Probably Pirates · Done" && facts.Developer is null, "blanks out");
            Check(DutyTips.Ids(new DutyDefinition(4, 1, 1036, "Sastasha", "Dungeons", 15)) == "Duty 4 · instance 1 · territory 1036", "the ids");
        }),
        ("Duty tips 1691: wiring: one body; the Duties page for every player, the deep dungeons' floors, a monster's duty; cached while hovered", () =>
        {
            var body = Read("src/TenshoAssistant/MainWindow.DutyTooltip.cs");
            Check(body.Contains("private void DrawDutyTooltip(DutyTipFacts facts,string? after=null)", StringComparison.Ordinal) && body.Contains("using(TooltipScope.Begin())", StringComparison.Ordinal), "the body");
            Check(body.Contains("cached.Duty==duty.DutyId && cached.Status==status && cached.Revision==localization.Revision", StringComparison.Ordinal), "built once while hovered");
            Check(body.Contains("developerMode ? DutyTips.Ids(duty):null", StringComparison.Ordinal) && body.Contains("localization.Get(\"DutyUi.UnlockedBy\")", StringComparison.Ordinal), "ids in developer mode only; the unlocking quest");
            var page = Read("src/TenshoAssistant/MainWindow.Duties.cs");
            Check(page.Contains("if(RichTipHovered())DrawDutyTooltip(DutyTipOf(duty),localization.Get(\"DutyUi.TipClick\"));", StringComparison.Ordinal) && !page.Contains("WrappedTooltip($\"Duty {duty.DutyId}", StringComparison.Ordinal), "the Duties page, for every player");
            Check(Read("src/TenshoAssistant/MainWindow.DeepDungeons.cs").Contains("if(RichTipHovered())DrawDutyTooltip(DutyTipOf(floor),", StringComparison.Ordinal), "the deep dungeons' floors");
            Check(Read("src/TenshoAssistant/MainWindow.Monsters.cs").Contains("if (duty is not null && RichTipHovered()) DrawDutyTooltip(DutyTipOf(duty),", StringComparison.Ordinal), "a monster's duty");
            foreach (var code in new[] { "en", "fr" })
            {
                var language = Read($"src/TenshoAssistant/Localization/Languages/{code}.json");
                Check(language.Contains("\"TipKind\": \"{0} · ", StringComparison.Ordinal) && language.Contains("\"TipClick\": \"", StringComparison.Ordinal), code + ": the texts");
            }
        }),
    ];
}
