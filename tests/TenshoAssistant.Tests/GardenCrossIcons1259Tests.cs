// 0.1259 (X-178, last version: crossbreeds in icons). Under each planted bed, one small icon per seed the bed may give, with a star
// (wished) and a disc (not held in what was read), « +N » beyond four, and a glyph with its sentence when there is no crossbreed or it
// is unknown. The whole patch tended from an icon button; a pot's seed has its small icon. The detailed view keeps the sentences.
internal static class GardenCrossIcons1259Tests
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
        ("Garden cross icons 1259: each possible seed is an icon with its marks, tooltip and menu; the rest is counted",() =>
        {
            var view=Read("src/TenshoAssistant/MainWindow.GardenView.cs");
            Check(Between(view,"private void DrawGardenGridIcons(","\n    }").Contains("DrawGardenCrossIcons(forecast,planted);"),"the icon view draws the crossbreeds as icons");
            var cross=Between(view,"private void DrawGardenCrossIcons(","\n    }");
            Check(cross.Contains("DrawItemIconOrText(item,new Vector2(side),Name(seed),Text,out var asIcon)") && cross.Contains("ItemLinksMenu(UniqueMenuId($\"GardenCross{seed}\"),item);")
                && cross.Contains("if(clicked)SelectItem(seed);") && cross.Contains("if(hovered)DrawGardenCrossTooltip("),"icon, menu, click, tooltip");
            Check(cross.Contains("GardenView.Mark(wished,holdings.HasObservation,holdings.Total)") && cross.Contains("RefreshRecipeHoldingsCache();"),"the mark comes from the wishlist and the inventories read");
            Check(view.Contains("private const int GardenCrossIconsShown=4;") && cross.Contains("forecast.Outcomes.Skip(shown).Select(Name)"),"four icons, the others counted and named in the tooltip");
            Check(cross.Contains("DrawGardenCrossNote(FontAwesomeIcon.Ban,localization.Get(\"Garden.ForecastNone\"))") && cross.Contains("DrawGardenCrossNote(FontAwesomeIcon.Question,localization.Get(\"Garden.ForecastUnknownSeed\"))")
                && cross.Contains("localization.Format(\"Garden.ForecastUnknownNeighbour\",forecast.Partner ?? 0,BedSeed(forecast.Partner))"),"no crossbreed and unknown: a glyph with the sentence of before");
            var note=Between(view,"private void DrawGardenCrossNote(","\n    }");
            Check(note.Contains("ImGui.InvisibleButton(\"##GardenCrossNote\"") && note.Contains("TipText(text);"),"the glyph is an element with its tooltip");
            var tooltip=Between(view,"private void DrawGardenCrossTooltip(","\n    }");
            Check(tooltip.Contains("if(!observed)ImGui.TextDisabled(localization.Get(\"Garden.CrossUnknown\"));") && tooltip.Contains("else ImGui.TextColored(Warning,localization.Get(\"Garden.CrossNotOwned\"));"),"never « not owned » without a reading");
            Check(tooltip.Contains("\"Garden.CrossWith\"") && tooltip.Contains("\"Garden.CrossWished\"") && tooltip.Contains("\"Garden.CrossOwned\"") && tooltip.Contains("\"Garden.CrossForecast\""),"partner, wish, count, and « a forecast »");
            Check(!System.Text.RegularExpressions.Regex.IsMatch(view,@"new (System\.Numerics\.)?Vector4\("),"no colour written by hand");
        }),
        ("Garden cross icons 1259: the whole patch from an icon button, the detailed view unchanged, texts in both languages",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Garden.cs");
            Check(page.Contains("configuration.GardenDetailedView ? ImGui.SmallButton(localization.Get(\"Garden.TendPatch\")):ToolButton(Dalamud.Interface.FontAwesomeIcon.CheckDouble,\"Garden.TendPatchTip\",true);"),"icon button in icons, words in the detailed view");
            Check(Between(page,"private void DrawGardenGridText(","\n    }").Contains("DrawGardenForecast(forecast,planted);"),"the detailed view keeps the sentences");
            Check(page.Contains("if(!configuration.GardenDetailedView && GardenSeedItem(bed) is { } potSeed)"),"a pot's seed has its small icon");
            foreach(var file in new[]{"en","fr"})
            {
                var garden=Between(Read($"src/TenshoAssistant/Localization/Languages/{file}.json"),"\n  \"Garden\": {","\n  },");
                foreach(var key in new[]{"CrossWith","CrossWished","CrossNotOwned","CrossOwned","CrossUnknown","CrossForecast"})Check(garden.Contains($"    \"{key}\": \""),$"{file}: Garden.{key}");
            }
        }),
    ];
}
