using TenshoAssistant.Discoveries;

// 0.1174 (X-159, user's treasure hunt of 12/11, docs/evidence/0.1174-defeat-treasure.md): a monster's drop put down to its kill read from the game's
// « defeat » message (row 557, its corpse found by name), within 6 s; a treasure hunt's chest put down to the deciphered map.
internal static class TreasureDefeat1174Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private sealed record Body(ulong Id, string Name, float Distance);
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Treasure 1174: a drop 3.7 s after the kill is the monster's; a treasure hunt's chest is the map's; a coffer opened stays a coffer", () =>
        {
            Check(ItemGains.Attribute(false, false, 13_700, -100_000, 0, 10_000, false, null, false, false) == ItemGains.Cause.MonsterDrop, "3.7 s after the kill (it was 3 s)");
            Check(ItemGains.Attribute(false, false, 17_000, -100_000, 0, 10_000, false, null, false, false) == ItemGains.Cause.None, "7 s after: not the kill");
            Check(ItemGains.Attribute(false, false, 17_600, -100_000, 0, 10_000, false, null, false, false, treasureHunt: true) == ItemGains.Cause.TreasureChest, "the chest, 7.6 s after the last kill");
            Check(ItemGains.Attribute(false, false, 13_000, -100_000, 0, 10_000, false, null, false, false, treasureHunt: true) == ItemGains.Cause.MonsterDrop, "a hunt's monster drop stays a drop");
            Check(ItemGains.Attribute(false, false, 17_600, -100_000, 0, -100_000, false, 5, true, false, treasureHunt: true) == ItemGains.Cause.Opened, "a coffer opened meanwhile");
            Check(ItemGains.Attribute(false, false, 17_600, -100_000, 0, -100_000, true, null, false, false, treasureHunt: true) == ItemGains.Cause.None, "in a duty (a portal): the loot window says it");
            Check(ItemGains.Attribute(false, true, 17_600, -100_000, 0, -100_000, false, null, false, false, treasureHunt: true) == ItemGains.Cause.None, "busy: nothing");
        }),
        ("Treasure 1174: « you defeat » is yours only when you are the source and the target is a monster; its corpse found by name, nearest, once", () =>
        {
            Check(ItemGains.DefeatedByMe(557, true, "Alpha Tester", false, "cocatrix territorial", "Alpha Tester"), "yours");
            Check(!ItemGains.DefeatedByMe(557, false, "sentinelle des vigiles sombres", false, "chigoe", "Alpha Tester"), "an NPC's kill");
            Check(!ItemGains.DefeatedByMe(557, true, "Other Player", false, "chigoe", "Alpha Tester"), "another player's kill");
            Check(!ItemGains.DefeatedByMe(557, true, "Alpha Tester", true, "Other Player", "Alpha Tester"), "a player defeated");
            Check(!ItemGains.DefeatedByMe(1367, true, "Alpha Tester", false, "chigoe", "Alpha Tester"), "another message");
            Body[] dead = [new(1, "Jeune triton territorial", 9), new(2, "jeune triton territorial", 4), new(3, "orobon territorial", 1)];
            var credited = new HashSet<ulong>();
            Check(ItemGains.Corpse(dead, body => body.Name, body => body.Distance, body => credited.Contains(body.Id), "jeune triton territorial")?.Id == 2, "nearest of that name, case ignored");
            credited.Add(2);
            Check(ItemGains.Corpse(dead, body => body.Name, body => body.Distance, body => credited.Contains(body.Id), "jeune triton territorial")?.Id == 1, "the next one once the first is credited");
            credited.Add(1);
            Check(ItemGains.Corpse(dead, body => body.Name, body => body.Distance, body => credited.Contains(body.Id), "jeune triton territorial") is null, "none left: not guessed");
            Check(ItemGains.Corpse(dead, body => body.Name, body => body.Distance, body => false, "bec-pic") is null, "no corpse of that name");
        }),
        ("Treasure 1174: wiring: the defeat message kept with its entities, read each pass, the hunt told by the game's director event, the map's item given", () =>
        {
            var watcher = Read("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs");
            Check(watcher.Contains("if(id==ItemGains.DefeatMessage)", StringComparison.Ordinal) && watcher.Contains("to.ObjStrId", StringComparison.Ordinal), "message kept");
            Check(watcher.Contains("ReadTarget(now,at,territory,map);ReadDefeats(at,territory,map,me);ReadTreasureHunt(now);", StringComparison.Ordinal), "read each pass");
            Check(watcher.Contains("EventHandlerContent.TreasureHuntDirector", StringComparison.Ordinal), "the director's event");
            Check(watcher.Contains("if(!creditedKills.Add(npc.GameObjectId))return;", StringComparison.Ordinal), "a corpse credited once");
            Check(watcher.Contains("store.SeeSource(DiscoveryKind.ItemFromItem,item,treasureMapItem,quantity,at,hq:hq)", StringComparison.Ordinal), "the chest noted from the map");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("itemWatcher.TreasureMapItem=", StringComparison.Ordinal), "the map's item given");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1174-defeat-treasure.md")), "evidence");
        }),
    ];
}
