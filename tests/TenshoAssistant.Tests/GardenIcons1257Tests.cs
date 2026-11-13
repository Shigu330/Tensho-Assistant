// 0.1257 (X-178, version 2: the garden patch in icons). Each bed is its seed's icon with a growth bar, the time left, a round mark and
// two icon buttons; « Detailed view » brings the text back. Wiring tests (the computations are tested by GardenView1256Tests; the page is
// drawn in both views by the desktop application's headless run, with beds given to its copy of the settings).
internal static class GardenIcons1257Tests
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
        ("Garden icons 1257: the setting chooses between the two views, icons by default",() =>
        {
            Check(!new TenshoAssistant.Configuration().GardenDetailedView,"icons by default");
            var configuration=Read("src/TenshoAssistant/Configuration.cs");
            Check(configuration.Contains("\"GardenAlerts\", \"GardenDetect\", \"GardenDetailedView\","),"in « restore every setting »");
            var page=Read("src/TenshoAssistant/MainWindow.Garden.cs");
            Check(page.Contains("BoolSetting(\"Garden.DetailedView\",\"Garden.DetailedViewTip\",()=>configuration.GardenDetailedView,value=>configuration.GardenDetailedView=value,c=>c.GardenDetailedView);"),"shared setting row in the journal");
            Check(page.Contains("if(configuration.GardenDetailedView)DrawGardenGridText(number,bed,planted,forecasts,now,ref changed,ref removed);")
                && page.Contains("else DrawGardenGridIcons(number,bed,planted,forecasts,now,ref changed,ref removed);"),"both paths, one table");
            var text=Between(page,"private void DrawGardenGridText(","\n    }");
            Check(text.Contains("ImGui.TextWrapped($\"{number}. {bed.Seed}\");") && text.Contains("ImGui.TextDisabled(GardenTimes(bed,now));") && text.Contains("DrawBedButtons(bed,now,ref changed,ref removed);")
                && text.Contains("DrawGardenForecast(forecast,planted);"),"the detailed view is the text of before: seed, dates, buttons, forecast");
            Check(page.Contains("\"###GardenJournal\",ImGuiTreeNodeFlags.DefaultOpen)"),"the journal is open when the page opens");
        }),
        ("Garden icons 1257: an icon cell is reached by the keyboard, says its state by a glyph or a word, and deletes in two clicks",() =>
        {
            var view=Read("src/TenshoAssistant/MainWindow.GardenView.cs");
            var cell=Between(view,"private void DrawGardenGridIcons(","\n    }");
            Check(cell.Contains("DrawItemIconOrText(item,new Vector2(side),bed.Seed,GardenColor(state),out asIcon)"),"the seed: icon or name");
            Check(cell.Contains("if(hovered)DrawGardenBedTooltip(bed,item,state,now);") && cell.Contains("ItemLinksMenu(UniqueMenuId($\"Garden{bed.Id}\"),item);")
                && cell.Contains("if(clicked && item is not null)SelectItem(item.Id);"),"tooltip, right-click menu, click = item in context");
            Check(cell.Contains("ImGui.InvisibleButton($\"##GardenEmpty{number}\",new Vector2(side));\n            TipText(localization.Format(\"Garden.EmptyBed\",number));"),"an empty bed is an element with its tooltip");
            Check(cell.Contains("if(!asIcon && badge==GardenBadge.Due)ImGui.TextColored(Warning,localization.Get(\"Garden.StateToTend\"));"),"without the icon, the word says a tending is due");
            Check(cell.Contains("ImGui.TextColored(Success,localization.Get(\"Garden.Ripe\"))"),"ripe in a word");
            var buttons=Between(view,"private void DrawGardenIconButtons(","\n    }");
            Check(buttons.Contains("ToolButton(FontAwesomeIcon.HandHoldingDroplet,\"Garden.TendTip\",GardenJournal.TendAt(bed) is not null)")
                && buttons.Contains("IconDeleteButton(FontAwesomeIcon.TrashAlt,\"Garden.Remove\",\"Garden.RemoveTip\")") && buttons.Contains("DesktopEditBegin();"),"tended and remove as icon buttons (D-112), greyed in the desktop application");
            var badge=Between(view,"private void DrawGardenBadge(","\n    }");
            Check(badge.Contains("GardenBadge.Due=>(Warning,background,FontAwesomeIcon.Exclamation)") && badge.Contains("GardenBadge.Ripe=>(Success,background,FontAwesomeIcon.Check)")
                && badge.Contains("_=>(background,Muted,FontAwesomeIcon.Tint)"),"each mark has its own glyph");
            Check(!System.Text.RegularExpressions.Regex.IsMatch(view,@"new (System\.Numerics\.)?Vector4\("),"no colour written by hand");
            var tooltip=Between(view,"private void DrawGardenBedTooltip(","\n    }");
            foreach(var key in new[]{"Garden.TipPlanted","Garden.TipRipeIn","Garden.TipTendNow","Garden.TipTendAt","Garden.TipClick","Garden.StateToTend","Garden.StateGrowing"})
                Check(tooltip.Contains($"\"{key}\""),"tooltip: "+key);
        }),
        ("Garden icons 1257: an item is an invisible button with its icon, or its name once the icon does not come",() =>
        {
            var cell=Between(Read("src/TenshoAssistant/UI/Components/ItemCell.cs"),"private bool DrawItemIconOrText(","\n    }");
            Check(cell.Contains("ImGui.InvisibleButton(\"##ItemIconOrText\",size)") && cell.Contains("ImGui.GetWindowDrawList().AddImage(texture.Handle,ImGui.GetItemRectMin(),ImGui.GetItemRectMax());"),"the icon on a button of its size");
            Check(cell.Contains("if(itemIconWait.ShowText(iconId,false,frame))") && cell.Contains("ImGui.Selectable(name+\"##ItemIconOrText\",false,ImGuiSelectableFlags.None,new Vector2(ImGui.CalcTextSize(name).X,0))") && cell.Contains("itemIconWait.ShowText(iconId,true,frame);"),"the wait decides, and starts again once drawn");
        }),
        ("Garden icons 1257: texts in both languages, the page's help and a FAQ entry",() =>
        {
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                var garden=Between(text,"\n  \"Garden\": {","\n  },");
                foreach(var key in new[]{"DetailedView","DetailedViewTip","EmptyBed","StateGrowing","StateToTend","TipPlanted","TipRipeIn","TipTendAt","TipTendNow","TipClick"})
                    Check(garden.Contains($"    \"{key}\": \""),$"{file}: Garden.{key}");
                Check(text.Contains("    \"QuestionGardenText\": \"") && text.Contains("    \"AnswerGardenText\": \""),file+": FAQ");
                Check(Between(text,"\"GardenStart\": \"","\n").Contains(file=="fr" ? "Vue détaillée":"Detailed view"),file+": the page's help names the detailed view");
            }
            Check(Read("src/TenshoAssistant/Help/Faq.cs").Contains("new(\"GardenText\", \"Garden\"),"),"FAQ entry");
        }),
    ];
}
