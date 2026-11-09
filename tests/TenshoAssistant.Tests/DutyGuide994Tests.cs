using TenshoAssistant.Duties;

// X-90 (0.994): a duty's wiki page cut into its fight sections; loot and story left out; hidden in combat.
internal static class DutyGuide994Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Duty guide 994: sections by heading, edit links gone, loot and story skipped, lists kept as lines",() =>
        {
            var html="<p>Intro</p><h2><span class=\"mw-headline\">Objectives</span><span class=\"mw-editsection\">[edit]</span></h2><ul><li>Clear the hall</li><li>Defeat the boss</li></ul>"
                +"<h3>Boss: Adjudicator</h3><p>Avoid the <b>red</b> circles.</p><h2>Loot</h2><p>Gear</p><h2>Story</h2><p>Long ago</p>";
            var sections=WikiDutyGuideParser.Sections(html);
            Check(sections.Count==2 && sections[0].Title=="Objectives" && sections[0].Text=="Clear the hall\nDefeat the boss","objectives: "+string.Join("|",sections.Select(row=>row.Title+"="+row.Text)));
            Check(sections[1].Title=="Boss: Adjudicator" && sections[1].Text=="Avoid the red circles.","boss");
        }),
        ("Duty guide 994: shown out of combat only, credited, on a click",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Duties.cs"));
            Check(page.Contains("if(InCombatNow?.Invoke()==true){EmptyState(\"DutyGuide.InCombat\");return;}"),"combat");
            Check(TenshoAssistant.Credits.CreditCatalog.All.Any(credit=>credit.Files.Contains("Duties/WikiDutyGuide.cs")),"credited");
        }),
    ];
}
