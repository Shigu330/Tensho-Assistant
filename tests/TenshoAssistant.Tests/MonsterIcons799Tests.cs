// 0.799 (X-38, user request): the Blue Mage icon above a monster's head, in the game's view.
internal static class MonsterIcons799Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Monster icons: above the head from the height read, within the distance, behind Tensho's windows, reasons chosen",() =>
        {
            var icons=Read("MainWindow.MonsterIcons.cs");
            Check(icons.Contains("gameGui.WorldToScreen(monster.Position+new Vector3(0,monster.Height+0.6f,0),out var screen)") && icons.Contains("ImGui.GetBackgroundDrawList()"),"placed, behind");
            Check(icons.Contains("if(Vector3.Distance(monster.Position,live.Position)>distance)continue;") && icons.Contains("if(settings.MonsterIconsHideInCombat && live.InCombat)return;"),"distance, combat");
            Check(icons.Contains("private const uint BlueMageIcon=62100+BlueMageJob;"),"Blue Mage job icon");
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.MonsterIconsAbove && settings.AboveBlueSpell && !settings.AboveList && !settings.AboveHuntLog && !settings.AboveFate && !settings.MonsterIconsHideInCombat,"defaults");
            Check(Read("Plugin.cs").Contains("(\"MonsterIcons\",mainWindow.DrawMonsterOverheadIcons),"),"drawn each frame");
            Check(Read("Maps/MapLiveReader.Dalamud.cs").Contains("var height=native==null || !float.IsFinite(native->Height) ? 0:Math.Clamp(native->Height,0,50);"),"height read, bounded");
        }),
    ];
}
