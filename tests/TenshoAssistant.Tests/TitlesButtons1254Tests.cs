// 0.1254 (X-154, version 3 of the visual base): titles and buttons. A page title is followed by space instead of a line; titles inside a
// page go through SubTitle; the main action of a block is a PrimaryButton; a deletion in a row of icons is an IconDeleteButton (two clicks,
// D-112) that shares its drawing with ToolButton.
internal static class TitlesButtons1254Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static string Between(string text,string from,string to)
    {
        var start=text.IndexOf(from,StringComparison.Ordinal);
        if(start<0)throw new Exception("not found: "+from);
        var end=text.IndexOf(to,start+from.Length,StringComparison.Ordinal);
        return end<0 ? text[start..]:text[start..end];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Titles and buttons 1254: a page title ends with space, and the pages' inner titles are sub-titles",() =>
        {
            var header=Between(Read("src/TenshoAssistant/UI/UiComponents.cs"),"public static void SectionHeader(","\n    }");
            Check(header.Contains("ImGui.Dummy(new Vector2(0, ImGui.GetStyle().ItemSpacing.Y*2));") && !header.Contains("ImGui.Separator()"),"space, no separator, under a page title");
            Check(Read("src/TenshoAssistant/MainWindow.Garden.cs").Contains("UiComponents.SubTitle(localization.Format(\"Garden.GridTitle\",zone,patch));"),"garden: the title of a patch");
            Check(Read("src/TenshoAssistant/MainWindow.HomeToday.cs").Contains("UiComponents.SubTitle(localization.Get(\"Home.TodayTitle\"));"),"Home: « Today »");
            var home=Read("src/TenshoAssistant/MainWindow.Home.cs");
            Check(home.Contains("UiComponents.SubTitle(localization.Format(\"Home.ContextItem\",contextItem.Name));"),"Home: the context item");
            Check(home.Contains("StateLine(UiState.Waiting,localization.Get(\"CraftSearch.Loading\"))") && home.Contains("StateLine(UiState.Waiting,localization.Get(\"Home.CraftChain\"))")
                && home.Contains("StateLine(UiState.Waiting,localization.Get(\"Home.Cactpot\"))"),"Home: running computations are « waiting »");
            Check(!home.Contains("ImGui.TextColored(Accent,localization.Get(\"Home.") && !home.Contains("ImGui.TextColored(Accent,localization.Format(\"Home."),"no accent text written by hand left on Home");
        }),
        ("Titles and buttons 1254: one square glyph button for tools and deletions, a primary button with its tooltip",() =>
        {
            var tool=Read("src/TenshoAssistant/UI/Components/ToolButton.cs");
            Check(Between(tool,"private bool ToolButton(","\n    }").Contains("GlyphButton($\"##{tooltipKey}\",icon,active,null)"),"ToolButton draws through GlyphButton");
            Check(Between(tool,"private bool GlyphButton(","\n    }").Contains("DrawCentredGlyph(icon,colour);"),"the glyph centred by one method");
            Check(tool.Split("glyph->X1-glyph->X0").Length==2,"the centring is written once (X-164)");
            var primary=Between(tool,"private bool PrimaryButton(","\n    }");
            Check(primary.Contains("ImGui.PushStyleColor(ImGuiCol.Button,UI.FfxivTheme.Selected);") && primary.Contains("ImGui.PushStyleColor(ImGuiCol.Border,UI.FfxivTheme.Gold);")
                && primary.Contains("ImGui.PopStyleColor(2);") && !primary.Contains("ImGuiCol.Text"),"selection background, accent border, text colour untouched");
            Check(primary.Contains("Tip(tipKey);") && primary.Contains("return clicked && enabled;"),"tooltip required, no click when greyed");
            var delete=Between(Read("src/TenshoAssistant/UI/Components/DeleteButton.cs"),"private bool IconDeleteButton(","\n    }");
            Check(delete.Contains("deleteConfirm.IsArmed(id,now)") && delete.Contains("return clicked && deleteConfirm.Click(id,now);"),"two clicks through DeleteConfirm (D-112)");
            Check(delete.Contains("GlyphButton(imguiId,icon,false,armed ? UI.FfxivTheme.Error:null)") && delete.Contains("Tip(armed ? \"Common.DeleteConfirm\":tipKey);"),"armed: the glyph in the error colour and « click again » in the tooltip");
            var garden=Read("src/TenshoAssistant/MainWindow.Garden.cs");
            Check(garden.Contains("if(PrimaryButton(\"Garden.Plant\",full ? \"Garden.Full\":\"Garden.PlantTip\",!full))"),"garden: planting is the form's main action");
        }),
    ];
}
