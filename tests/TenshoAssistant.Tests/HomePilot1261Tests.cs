using TenshoAssistant.Reminders;

// 0.1261 (X-155, pilot of the visual plan, last two screens). Home: the nearest deadline first, one click to its tool, and tiles that are
// one action each. Items: the wishlist from the item's header, « Add » turning into « ✓ On my wishlist » on the same spot once the save is
// confirmed. The brief outlines follow Dalamud's « reduced motion » preference (UI/Motion.cs, tested by WeatherBanner1260Tests).
internal static class HomePilot1261Tests
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
    private static readonly DateTimeOffset Now=new(2026,11,13,14,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Home pilot 1261: the next deadline is the nearest still to come",() =>
        {
            var alarm=new HomeDeadline("Alarms","Node",Now.AddMinutes(20));
            var vessel=new HomeDeadline("Vessels","a vessel returns",Now.AddMinutes(5));
            var passed=new HomeDeadline("Garden","Mandrake ripe",Now.AddMinutes(-1));
            Check(HomeToday.Next([alarm,vessel,passed],Now)==vessel,"the nearest, never one that passed");
            Check(HomeToday.Next([passed],Now) is null && HomeToday.Next([],Now) is null && HomeToday.Next(null,Now) is null,"none");
            Check(HomeToday.Next([new("Vessels","b",Now.AddMinutes(5)),new("Alarms","z",Now.AddMinutes(5)),new("Alarms","a",Now.AddMinutes(5))],Now) is { Kind:"Alarms",Label:"a" },"same time: a stable choice");
            Check(HomeToday.Next([new("Alarms","now",Now)],Now) is null,"a deadline at this very instant has passed");
        }),
        ("Home pilot 1261: Home shows it first, hidden like the other lines, and a tile is one action",() =>
        {
            var today=Read("src/TenshoAssistant/MainWindow.HomeToday.cs");
            Check(today.Contains("HomeTodayLines=[\"Next\",\"Reminders\",\"Finished\",\"Vessels\",\"Quests\",\"Alarms\"];"),"a line of « Customize », first");
            Check(today.IndexOf("DrawHomeNextDeadline(now);",StringComparison.Ordinal)<today.IndexOf("foreach(var (key,text,target,good) in homeToday)",StringComparison.Ordinal),"drawn before the other lines");
            var build=Between(today,"private void BuildHomeToday(","\n    }");
            Check(build.Contains("new(\"Alarms\",nextAlarm.Alarm.Label,nextAlarm.At)") && build.Contains("Voyages.NextReturn(configuration.Voyages,now)") && build.Contains("configuration.GardenAlerts && GardenJournal.Alarms(configuration.GardenBeds,now)"),"alarms, vessels, garden (when its reminders are on)");
            Check(build.Contains("if(homeNextShown is not null && shown is not null)homeNextChangedAt=Environment.TickCount64;"),"the outline starts when another deadline takes the place, not at the first one");
            var draw=Between(today,"private void DrawHomeNextDeadline(","\n    }");
            Check(draw.Contains("configuration.HomeTodayHidden?.Contains(\"Next\")==true") && draw.Contains("next.At<=now"),"hidden by the player, or passed: not drawn");
            Check(draw.Contains("left<TimeSpan.FromHours(1) ? TenshoAssistant.UI.Motion.Countdown(left)") && draw.Contains("Tip(\"Home.NextDeadlineTip\");"),"under an hour it ticks; its tooltip");
            Check(draw.Contains("Motion.Pulse(homeNextChangedAt,Environment.TickCount64,HomeNextPulseMs,ReducedMotion)"),"brief outline, cut by reduced motion");
            var tiles=Between(Read("src/TenshoAssistant/MainWindow.Home.cs"),"private void DrawHomeTiles()","\n    }");
            Check(tiles.Contains("ImGui.Selectable(\"##Tile\",false,ImGuiSelectableFlags.None,new Vector2(0,side))") && !tiles.Contains("ImGui.Button("),"one element per tile (it had a button and a name)");
            Check(tiles.Contains("draw.PushClipRect(min,max,true);") && tiles.Contains("finally{draw.PopClipRect();}"),"its texts stay inside the tile");
        }),
        ("Home pilot 1261: the item's header adds to the wishlist and confirms on the spot, only once saved",() =>
        {
            var wish=Read("src/TenshoAssistant/MainWindow.Wishlist.cs");
            var button=Between(wish,"private void DrawWishButton(ItemDefinition item)","\n    }");
            Check(button.Contains("MathF.Max(ImGui.CalcTextSize(add).X,ImGui.CalcTextSize(inList).X+glyph+style.ItemInnerSpacing.X)"),"one width for both labels");
            Check(button.Contains("Tip(wished ? \"Wishlist.InListTip\":\"Wishlist.AddTip\");") && button.Contains("DesktopEditBegin();"),"tooltip in both states; greyed in the desktop application");
            Check(button.Contains("FontAwesomeIcon.Check") && button.Contains("Motion.Pulse(wishConfirmed.At,Environment.TickCount64,WishConfirmPulseMs,ReducedMotion)"),"a check from the icon font; a brief outline, cut by reduced motion");
            var toggle=Between(wish,"private void ToggleWish(uint itemId)","\n    }");
            Check(toggle.Contains("added&=configuration.Wishlist?.Contains(itemId)==true;") && toggle.Contains("if(added)wishConfirmed=(itemId,Environment.TickCount64);"),"« added » is said only when the saved list holds the item");
            Check(Read("src/TenshoAssistant/MainWindow.ItemSheet.cs").Contains("if(item is not null)DrawWishButton(item);"),"in the item's header");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                foreach(var key in new[]{"NextDeadline","NextDeadlineTip","DeadlineVessel","DeadlineRipe","DeadlineTend","TodayLineNext","InList","InListTip","AddTip"})Check(text.Contains($"    \"{key}\": \""),$"{file}: {key}");
            }
        }),
    ];
}
