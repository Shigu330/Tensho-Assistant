using System.Numerics;
using TenshoAssistant.Maps;
using TenshoAssistant.Reminders;

// X-387 and X-395 (0.1366): a chat map link hovered long ago or far from the cursor is not what a right click is about; the clicked link is
// given to the Tensho map once; the right click is read from Windows and traced; a garden menu followed by a planting, or closed and
// followed by a gain line later, is no harvest.
internal static class ChatLinkGarden1366Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly HoveredMapLink Link = new(140, 20, 22.4f, 21.7f, "Thanalan occidental");
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
    private const string Here = "Shirogane 2-17";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-395 M4: a link whose mouse out never came is not kept for ever, nor far from where it was hovered", () =>
        {
            var hover = new ChatLinkHover();
            var at = new Vector2(300, 900);
            hover.Over(Link, 10_000, at);
            Check(hover.RightClick(12_000, at + new Vector2(5, 3), cut: false, enabled: true) == Link, "still on the link (no mouse out): the menu");
            Check(hover.RightClick(12_000, at + new Vector2(400, -300), cut: false, enabled: true) is null, "right click in the world, far from the link: no menu");
            Check(hover.Describe(12_000, at + new Vector2(400, -300))!.StartsWith("map 20 not taken: cursor 500 px away", StringComparison.Ordinal), "traced");
            Check(hover.RightClick(10_000 + ChatLinkHover.MaxAge + 1, at, cut: false, enabled: true) is null, "hovered too long ago: no menu");
            Check(hover.Describe(10_000 + ChatLinkHover.MaxAge + 1, at)!.Contains("hovered", StringComparison.Ordinal), "traced: too old");
            hover.Over(Link, 80_000, at);
            Check(hover.Describe(80_010, at) == "map 20, menu opened", "a new hover refreshes it");
            Check(new ChatLinkHover().Describe(1, at) is null, "no line when no map link was hovered (camera turns)");
        }),
        ("X-387: the link clicked with the left button goes to the Tensho map once, within its life, never during a cut", () =>
        {
            var hover = new ChatLinkHover();
            hover.Clicked(Link, 5_000);
            Check(hover.TakeClick(5_020, cut: false, enabled: true) == Link && hover.TakeClick(5_030, cut: false, enabled: true) is null, "once");
            hover.Clicked(Link, 5_000);
            Check(hover.TakeClick(5_000 + ChatLinkHover.ClickLife + 1, cut: false, enabled: true) is null, "too old");
            hover.Clicked(Link, 5_000);
            Check(hover.TakeClick(5_010, cut: true, enabled: true) is null, "cut: nothing");
            hover.Clicked(Link, 5_000);
            Check(hover.TakeClick(5_010, cut: false, enabled: false) is null, "chat links off: nothing");
        }),
        ("X-387: the click read from Windows, traced at the click, the Tensho map moved to the link's map", () =>
        {
            var watcher = Read("src/TenshoAssistant/Maps/ChatLinkHoverWatcher.Dalamud.cs");
            Check(watcher.Contains("LinkMouseClick=75") && watcher.Contains("if(kind==LinkMouseClick){Click(args.AddonName,received.AtkEventData);return;}"), "the link click event");
            Check(watcher.Contains("log.Information(\"Chat link clicked ({0}): map link, map {1}, {2} button.\""), "a line at the click");
            Check(watcher.Contains("Hover.Over(link,Environment.TickCount64,NativeMouse.Cursor());"), "the cursor kept with the hover");
            var window = Read("src/TenshoAssistant/MainWindow.ChatMapLinks.cs");
            Check(window.Contains("var down=NativeMouse.RightDown();var pressed=down && !chatRightWasDown;chatRightWasDown=down;"), "the press from Windows");
            Check(window.Contains("log.Information(\"Chat link right click: {0}.\",traced)"), "a line at the right click");
            Check(window.Contains("hover.TakeClick(now,cut,enabled) is { } clicked && MapViewVisible()") && window.Contains("mapViewMapId=clickedMap.MapId;mapViewFollow=false;"), "the open Tensho map follows the link");
            var mouse = Read("src/TenshoAssistant/Maps/NativeMouse.Dalamud.cs");
            foreach (var call in new[] { "SendInput", "mouse_event", "SetCursorPos", "PostMessage", "SendMessage" })
                Check(!mouse.Contains(call, StringComparison.Ordinal), "read only: " + call);
        }),
        ("X-395 M3: a planting forgets the bed menu, a menu closed without anything is forgotten after 4 s", () =>
        {
            var t = new DateTimeOffset(2026, 11, 15, 2, 0, 0, TimeSpan.Zero);
            var menu = new GardenMenuMemory();
            bool Harvest(DateTimeOffset at) => GardenChat.IsHarvest(62, "", 0, menu.Peek(Here, at, GardenMenuMemory.HarvestWindow) is not null);
            // Menu opened then closed without anything: a gain line 5 s later (a purchase, a delivery) is no harvest.
            menu.Seen(1, 3, Here, t);
            Check(!Harvest(t.AddSeconds(5)), "menu closed, gain 5 s later: no harvest");
            // Menu, then a planting: the next gain line, 2 s later, is no harvest of that bed.
            menu.Seen(1, 4, Here, t.AddSeconds(20));
            menu.Forget();
            Check(!Harvest(t.AddSeconds(22)), "planted: the menu is forgotten");
            // A real harvest stays: the gain 1.5 s after the menu (log of 14/11).
            menu.Seen(1, 6, Here, t.AddSeconds(40));
            Check(Harvest(t.AddSeconds(41.5)), "a harvest 1.5 s after the menu");
            var watcher = Read("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs");
            Check(watcher.Contains("menuMemory.Forget();\n            planted(found,seed,Place());") || watcher.Contains("menuMemory.Forget();\r\n            planted(found,seed,Place());"), "wired at the planting");
        }),
    ];
}
