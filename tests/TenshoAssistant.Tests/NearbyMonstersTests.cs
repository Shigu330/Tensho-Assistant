// Test audit of 2026-11-09, batch 2 (0.1021): NearbyMonsters795Tests, MonsterIcons799Tests, MonsterUses1002Tests, HuntNearby796Tests merged into NearbyMonstersTests; every case is kept.
using TenshoAssistant.Monsters;
using TenshoAssistant.Hunting;
using TenshoAssistant.UI;
internal static class NearbyMonstersTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static HuntSlot Slot(int rank,params byte[] first)=>new(rank,[first,new byte[4],new byte[4],new byte[4],new byte[4],new byte[4],new byte[4],new byte[4],new byte[4],new byte[4]]);
    private static string ReadLf(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From NearbyMonsters795Tests: 0.795 (X-34, X-35, user request): nearby monsters that drop what the lists still need.
        ("Nearby monsters: only the drops still needed, each once; names per item",() =>
        {
            var drops=new Dictionary<uint,IReadOnlyList<uint>>{[10]=[100,101,100],[11]=[101],[12]=[102]};
            IReadOnlyList<uint> Of(uint id)=>drops.TryGetValue(id,out var list) ? list:[];
            var needed=new HashSet<uint>{100,101};
            Check(NearbyMonsters.ListDrops(10,Of,needed).SequenceEqual(new uint[]{100,101}),"each once");
            Check(NearbyMonsters.ListDrops(12,Of,needed).Count==0 && NearbyMonsters.ListDrops(10,Of,new HashSet<uint>()).Count==0,"nothing needed");
            var by=NearbyMonsters.DroppedNearby([(10,"Wolf"),(11,"Bat"),(10,"Wolf")],Of,needed);
            Check(by[101].SequenceEqual(new[]{"Wolf","Bat"}) && by[100].SequenceEqual(new[]{"Wolf"}) && !by.ContainsKey(102),"names");
        }),
        ("Nearby monsters: drops indexed by monster from MobDrop",() =>
        {
            var csv=new Dictionary<string,string>{["MobDrop"]="ItemId,BNpcNameId\n5000,42\n5001,42\n5000,43\n"};
            var data=new TenshoAssistant.Community.LuminaSupplementalData(csv);
            Check(data.ItemsDroppedBy(42).SequenceEqual(new uint[]{5000,5001}) && data.ItemsDroppedBy(43).SequenceEqual(new uint[]{5000}) && data.ItemsDroppedBy(44).Count==0,"index");
        }),
        ("Nearby monsters: living enemies only, bounded, behind their settings, information only",() =>
        {
            var reader=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("npc.BattleNpcKind!=Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind.Combatant || !npc.IsTargetable") && reader.Contains("if(result.Count>=100)break;"),"enemies, bounded");
            Check(Read("MainWindow.MapView.cs").Contains("if((settings.ShowListMonsters || settings.ShowHuntLogMonsters || settings.ShowFateMonsters || settings.ShowBlueMageMonsters || settings.ShowCollectionMonsters || settings.ShowBeastMonsters || settings.ShowRelicMonsters || settings.ShowWishMonsters) && live?.Monsters is { Count:>0 } monsters"),"map setting");
            Check(Read("MainWindow.ListPanels.cs").Contains("if(nearbyDroppedItems.TryGetValue(itemId,out var droppers))"),"list line");
        }),
        // From MonsterIcons799Tests: 0.799 (X-38, user request): the Blue Mage icon above a monster's head, in the game's view.
        ("Monster icons: above the head from the height read, within the distance, behind Tensho's windows, reasons chosen",() =>
        {
            var icons=Read("MainWindow.MonsterIcons.cs");
            Check(icons.Contains("gameGui.WorldToScreen(monster.Position+new Vector3(0,monster.Height+0.6f,0),out var screen)") && icons.Contains("ImGui.GetBackgroundDrawList()"),"placed, behind");
            Check(icons.Contains("if(Vector3.Distance(monster.Position,live.Position)>distance)continue;") && icons.Contains("if(settings.MonsterIconsHideInCombat && live.InCombat)return;"),"distance, combat");
            Check(icons.Contains("BlueMageIcon=62100+BlueMageJob"),"Blue Mage job icon");
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.MonsterIconsAbove && settings.AboveBlueSpell && !settings.AboveList && !settings.AboveHuntLog && !settings.AboveFate && !settings.MonsterIconsHideInCombat,"defaults");
            Check(Read("Plugin.cs").Contains("(\"MonsterIcons\",mainWindow.DrawMonsterOverheadIcons),"),"drawn each frame");
            Check(Read("Maps/MapLiveReader.Dalamud.cs").Contains("var height=native==null || !float.IsFinite(native->Height) ? 0:Math.Clamp(native->Height,0,50);"),"height read, bounded");
        }),
        // From MonsterUses1002Tests: X-40 (0.1002, DEC-049): three more uses of the nearby monsters: every crafting list, the relic book's targets, the wishes' drops.
        ("Monster uses 1002: new kinds with their layer, icon above and glyph; on by default; all-lists inside the list layer",() =>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.ListMonstersAllLists && settings.ShowRelicMonsters && settings.AboveRelic && settings.ShowWishMonsters && settings.AboveWish,"defaults");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("MonsterNoteKind.Relic=>settings.ShowRelicMonsters,") && map.Contains("MonsterNoteKind.Wish=>settings.ShowWishMonsters,"),"layers");
            var nearby=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.NearbyMonsters.cs"));
            Check(nearby.Contains("if(configuration.MapView.ListMonstersAllLists)needed.UnionWith(AllCraftListNeeds());"),"all lists");
            Check(nearby.Contains("NearbyMonsters.ListDrops(id,Drops,wishes)") && nearby.Contains("relicNeeds.TryGetValue(monster.Name.Trim(),out var left)"),"wishes, relic");
        }),
        // From HuntNearby796Tests: 0.796 (X-36, user request): nearby monsters the hunting log still asks for.
        ("Hunting log missing: current rank only, targets not done, by monster",() =>
        {
            // Gladiator (key 1, slot 0): rank 0 entry 1 with two targets; rank 1 entry 1.
            var entries=new[]{new HuntEntry(10001,"Gladiateur 01",[new HuntTarget("bombo",3,"",900),new HuntTarget("rat",2,"",901)]),new HuntEntry(10011,"Gladiateur 11",[new HuntTarget("loup",3,"",902)])};
            var slots=Enumerable.Repeat(Slot(0,3,1),12).ToArray();
            var missing=HuntingLog.Missing(entries,slots,null);
            Check(!missing.ContainsKey(900) && missing[901].Single()==("Gladiateur 01",1,2),"done left out, the rest with its kills");
            Check(!missing.ContainsKey(902),"rank not reached: not counted by the game");
            Check(HuntingLog.Missing(entries,[],null).Count==0,"not read: nothing");
            Check(HuntingLog.Missing([new HuntEntry(10001,"x",[new HuntTarget("old",3,"")])],slots,null).Count==0,"no monster row: nothing");
        }),
        ("Hunting log missing: the monster row comes from the sheet; one map layer, set on the map and on the log page",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Plugin.cs").Contains(".Where(name=>name.Length>0)),pair.target.Value.BNpcName.RowId))"),"name row");
            Check(Read("MainWindow.HuntingLog.cs").Contains("()=>configuration.MapView.ShowHuntLogMonsters"),"page setting");
            Check(Read("MainWindow.NearbyMonsters.cs").Contains("HuntingLog.Missing(entries,slots,HuntGrandCompany?.Invoke())"),"used");
        }),
        ("0.551: a monster's map button places the flag at its known position, and says where it comes from",() =>
        {
            var monsters=ReadLf("MainWindow.Monsters.cs");
            Check(monsters.Contains("if (monster.X is { } x && monster.Y is { } y)") && monsters.Contains("FlagFromMap(chosen,x,y);") && monsters.Contains("mapX=20.5f"),"known position, else as before");
        }),
    ];
}
