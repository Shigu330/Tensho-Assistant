// 0.878 (X-75.4): monsters seen casting a blue spell, with their level and place, shown before the wiki.
using TenshoAssistant.Discoveries;
internal static class BlueCasters878Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Blue casters: battle NPCs casting a blue spell's action, once per cast, bounded; shown first in the spell window",() =>
        {
            Check(DiscoveryStore.HasPlace(DiscoveryKind.BlueSpellCaster),"has a place");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("IBattleNpc { BattleNpcKind:BattleNpcSubKind.Combatant,IsCasting:true,NameId:not 0 }") && watcher.Contains("casting.Count>=64"),"monsters casting, bounded");
            Check(watcher.Contains("if(castsSeen.GetValueOrDefault(npc.GameObjectId)==action)continue;"),"once per cast");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.BlueSpellSources.cs"));
            Check(window.IndexOf("DrawBlueSpellSeen(spellId);",StringComparison.Ordinal) < window.IndexOf("if(BlueSpellWiki is not { } wiki)return;",window.IndexOf("private void DrawBlueSpellMonsters",StringComparison.Ordinal),StringComparison.Ordinal),"seen first");
        }),
    ];
}
