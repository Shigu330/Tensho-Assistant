// Test audit of 2026-11-09, batch 2 (0.1021): BlueCasters878Tests, BlueDuty930Tests, BlueMonsterOrder390Tests, BlueNearby798Tests, BlueSpellIndex395Tests, BlueSpellName387Tests, BlueSpellSources367Tests merged into BlueMagicTests; every case is kept.
using TenshoAssistant.Discoveries;
using TenshoAssistant.Collections;
using TenshoAssistant.Monsters;
internal static class BlueMagicTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From BlueCasters878Tests: 0.878 (X-75.4): monsters seen casting a blue spell, with their level and place, shown before the wiki.
        ("Blue casters: battle NPCs casting a blue spell's action, once per cast, bounded; shown first in the spell window",() =>
        {
            Check(DiscoveryStore.HasPlace(DiscoveryKind.BlueSpellCaster),"has a place");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("IBattleNpc { BattleNpcKind:BattleNpcSubKind.Combatant,IsCasting:true,NameId:not 0 }") && watcher.Contains("casting.Count>=64"),"monsters casting, bounded");
            Check(watcher.Contains("if(castsSeen.GetValueOrDefault(npc.GameObjectId)==action)continue;"),"once per cast");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.BlueSpellSources.cs"));
            Check(window.IndexOf("DrawBlueSpellSeen(spellId);",StringComparison.Ordinal) < window.IndexOf("if(BlueSpellWiki is not { } wiki)return;",window.IndexOf("private void DrawBlueSpellMonsters",StringComparison.Ordinal),StringComparison.Ordinal),"seen first");
        }),
        // From BlueDuty930Tests: X-76 P5 (0.930): a blue-magic spell learned in a duty shows the duty's level, read from ContentFinderCondition.ClassJobLevelRequired.
        ("Blue duty 930: duty level kept on the entry, read from the duty row, shown after the place",() =>
        {
            Check(new CollectionEntry(CollectionKind.BlueMagic,1,"S",0,false,DutyLevel:50).DutyLevel==50 && new CollectionEntry(CollectionKind.Mount,1,"M",0,true).DutyLevel==0,"field");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Collections/CollectionReader.Dalamud.cs")).Contains("dutyLevel = content.ClassJobLevelRequired;"),"read");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Collections.cs")).Contains("localization.Format(\"BlueSpell.DutyLevel\",spell.DutyLevel)"),"shown");
        }),
        // From BlueMonsterOrder390Tests: 0.390 (user request): the blue magic spell's monsters sorted by level — a range counts by its first number, no level stays last.
        ("Blue spell monsters: level order, ranges by their first number, rows without a level last, wiki order kept otherwise",() =>
        {
            IReadOnlyList<WikiMonsterRow> rows = [new("Ark Morbol","60","The Void Ark"),new("Stroper","","[[]]"),new("Morbol","43-48","[[]]"),
                new("Capricious Cassie","43","East Shroud"),new("Polluting Morbol","90","Urqopacha")];
            Check(BlueSpellSources.Level("43-48")==43 && BlueSpellSources.Level("")==null && BlueSpellSources.Level("?")==null,"levels");
            Check(BlueSpellSources.SortByLevel(rows,1).Select(row=>row.Name).SequenceEqual(["Morbol","Capricious Cassie","Ark Morbol","Polluting Morbol","Stroper"]),"lowest first, ties in wiki order");
            Check(BlueSpellSources.SortByLevel(rows,2).Select(row=>row.Name).SequenceEqual(["Polluting Morbol","Ark Morbol","Morbol","Capricious Cassie","Stroper"]),"highest first");
            Check(ReferenceEquals(BlueSpellSources.SortByLevel(rows,0),rows),"wiki order");
        }),
        // From BlueNearby798Tests: 0.798 (X-38, user request): nearby monsters that teach a blue magic spell not learned yet.
        ("Blue spell monsters: the wiki's English names matched to monster rows, each spell once",() =>
        {
            var names=new Dictionary<string,uint[]>(StringComparer.OrdinalIgnoreCase){["Tiny Mandragora"]=[10,11],["Kraken"]=[20]};
            IReadOnlyList<uint> Ids(string name)=>names.TryGetValue(name,out var ids) ? ids:[];
            var bySpell=new Dictionary<uint,IReadOnlyList<WikiMonsterRow>>{[1]=[new("tiny mandragora","5","x"),new("Unknown thing","1","y")],[2]=[new(" Kraken ","50","z"),new("Tiny Mandragora","5","x")]};
            var map=BlueSpellMonsterIndex.Map(bySpell,Ids);
            Check(map[10].SequenceEqual(new uint[]{1,2}) && map[11].SequenceEqual(new uint[]{1,2}) && map[20].SequenceEqual(new uint[]{2}),"matched, case ignored, trimmed");
            Check(map.Count==3,"an unknown name gives nothing");
        }),
        ("Blue spell monsters: pages read on a click only, three seconds apart; as Blue Mage unless always; settings on the map and the Blue magic page",() =>
        {
            var index=Read("Monsters/BlueSpellMonsterIndex.cs");
            Check(index.Contains("await Task.Delay(WikiMonsterSources.RequestInterval+TimeSpan.FromMilliseconds(100),token)") && index.Contains("public void LoadCached(") && index.Contains("ReadCachedAsync"),"cadence, disk first");
            Check(Read("MainWindow.BlueSpellSources.cs").Contains("if(SourceButton(localization.Format(\"BlueSpellSources.NearbyFetch\",missing),SourceOn(SourceUse.WikiBlueMagic),true))index.FetchMissing(spells);"),"click only");
            Check(Read("MainWindow.NearbyMonsters.cs").Contains("(configuration.MapView.BlueMageMonstersAlways || characterStats.State.JobId==BlueMageJob)"),"job");
            Check(!new TenshoAssistant.MapViewSettings().BlueMageMonstersAlways,"off by default");
        }),
        // From BlueSpellIndex395Tests: 0.395 (user request): every blue magic spell sorted by the level of its teaching monster, from the wiki's spellbook table (one page
        // for every spell, read once on opening, copy first, wiki switch honoured); unknown levels last.
        ("Blue spellbook: table parsed by number, monster level = lower of world and duty, whole list sorted, read once with a copy",() =>
        {
            var html = "<table class=\\\"sortable\\\"><tr><th>Spell</th><th>No.</th><th>Rank</th><th>Type</th><th>Min Lv</th><th>World Mob L</th><th>Duty L</th><th>Common Acquisition Methods</th></tr>"
                + "<tr><td>Water Cannon</td><td>1</td><td>x</td><td>Default</td><td>1</td><td>~</td><td>~</td><td>Default</td></tr>"
                + "<tr><td>Drill Cannons</td><td>5</td><td>x</td><td>Open World Dungeon</td><td>1</td><td>46</td><td>50</td><td>Magitek</td></tr>"
                + "<tr><td>Flame Thrower</td><td>2</td><td>x</td><td>Dungeon</td><td>50</td><td>~</td><td>50</td><td>Gobmachine</td></tr></table>";
            var json = "{\"parse\":{\"title\":\"Blue Magic Spellbook\",\"revid\":1548695,\"text\":{\"*\":\"" + html + "\"}}}";
            var book = BlueSpellbookParser.Parse(json,DateTimeOffset.UtcNow);
            Check(book.Rows.Count==3 && book.Rows[5].MonsterLevel==46 && book.Rows[2].MonsterLevel==50 && book.Rows[1].MonsterLevel is null && book.Revision==1548695,"parsed");
            int[] numbers=[1,2,5];
            Check(BlueSpellSources.SortByMonsterLevel(numbers,number=>book.Rows[number].MonsterLevel,1).SequenceEqual([5,2,1]),"lowest first, unknown last");
            Check(BlueSpellSources.SortByMonsterLevel(numbers,number=>book.Rows[number].MonsterLevel,2).SequenceEqual([2,5,1]),"highest first");
            var index = Read("Monsters/BlueSpellWikiIndex.cs");
            Check(index.Contains("disk.ReadAsync<BlueSpellbook>(Family,\"all\",Version,token)",StringComparison.Ordinal) && index.Contains("if(!enabled())",StringComparison.Ordinal),"copy first, wiki switch");
        }),
        // From BlueSpellName387Tests: 0.387 (user report: the blue magic spell window never gives the monster): the English spell name comes from the English Action
        // sheet by row (the AozAction → Action link followed the client's language), old cached answers are not read again, and a missing
        // wiki page is said as such. The real Bad Breath page (docs/evidence/0.387-blue-spell-name.md) parses into its monsters.
        ("Blue spell wiki: English Action sheet by row, new cache family, missing page said, a real table parsed",() =>
        {
            var plugin = Read("Plugin.cs");
            Check(plugin.Contains("GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.English).GetRowOrDefault(aoz.Action.RowId)",StringComparison.Ordinal)
                && !plugin.Contains("?.Action.ValueNullable?.Name.ExtractText()",StringComparison.Ordinal),"English name by row");
            Check(plugin.Contains("family:\"wiki-blue-spells-2\"",StringComparison.Ordinal),"old empty answers dropped");
            Check(Read("MainWindow.BlueSpellSources.cs").Contains("document.Revision==0",StringComparison.Ordinal),"missing page said");
            var html = "<table class=\\\"item sortable stdt__table_v2\\\"><tr><th>Enemy</th><th>Level</th><th>Location</th></tr>"
                + "<tr><td>Capricious Cassie</td><td>43</td><td>East Shroud (23.1,20.4)</td></tr></table>";
            var json = "{\"parse\":{\"title\":\"Bad Breath\",\"revid\":1537705,\"text\":{\"*\":\"" + html + "\"}}}";
            var document = TenshoAssistant.Monsters.WikiMonsterParser.Parse(json,28,"Bad Breath",DateTimeOffset.UtcNow);
            Check(document.Monsters.Count==1 && document.Monsters[0].Name=="Capricious Cassie","parsed");
            var missing = TenshoAssistant.Monsters.WikiMonsterParser.Parse("{\"error\":{\"code\":\"missingtitle\"}}",28,"Mauvaise haleine",DateTimeOffset.UtcNow);
            Check(missing.Revision==0 && missing.Monsters.Count==0,"missing page = revision 0");
        }),
        // From BlueSpellSources367Tests: 0.367 (g, C-13): blue magic sources from the wiki spell page — same table as item drops, place and coordinates parsed.
        ("Blue spell sources: wiki locations give a place and map coordinates; the wiki switch is honoured",() =>
        {
            Check(BlueSpellSources.Parse("East Shroud (23.1,20.4)") is { Place:"East Shroud",X:23.1f,Y:20.4f },"place and coordinates");
            Check(BlueSpellSources.Parse("Urqopacha (8.5,23.4,2.7)") is { Place:"Urqopacha",X:8.5f,Y:23.4f },"height ignored");
            Check(BlueSpellSources.Parse(" The Void Ark") is { Place:"The Void Ark",X:null,Y:null },"duty without coordinates");
            // 0.1019 (test audit #9): the cache family and the spell table are checked in the BlueSpellName387 cases of this file.
            Check(Read("Plugin.cs").Contains("Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.WikiBlueMagic)",StringComparison.Ordinal),"wiki switch honoured");
        }),
    ];
}
