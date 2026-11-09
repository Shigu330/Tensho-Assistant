// Test audit of 2026-11-09, batch 2 (0.1021): HuntAlert446Tests, HuntBills440Tests, HuntMarks441Tests, HuntTrain442Tests, HuntingLog364Tests, HuntingLogCompany393Tests merged into HuntTests; every case is kept.
using TenshoAssistant.Reminders;
using TenshoAssistant.Maps;
using TenshoAssistant.Hunting;
internal static class HuntTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From HuntAlert446Tests: N-09 / X-13 (0.446): the A and S mark alert is a setting off by default, said once per mark; the conductor's call is only copied.
        ("Hunt marks: A/S alert off by default, reset, once per mark; the train call is copied with <flag> (also sent on click: Decisions447Tests)",() =>
        {
            Check(!new TenshoAssistant.Configuration().HuntMarkAlert && TenshoAssistant.ConfigurationReset.Reset.Contains("HuntMarkAlert"),"setting off by default and in the reset list");
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.HuntTrain.cs"));
            Check(ui.Contains("item.Kind==MapObjectKind.HuntMark && item.Level>=2") && ui.Contains("huntMarksAnnounced.Add("),"A and S only, once per mark");
            Check(ui.Contains("if(configuration.AlarmOptions.PlaySound)Alarms.AlarmService.PlaySound("),"sound follows the alarm options");
            // 0.447: the call is also sent on click (decided 2026-11-03, Decisions447Tests); the copy stays.
            Check(ui.Contains("localization.Format(\"HuntTrain.AnnounceText\"") && ui.Contains("CopyButton(localization.Get(\"HuntTrain.Announce\"),()=>announce+\" <flag>\")"),"copied with <flag>");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.ChatNotice=text=>chat.Print(\"[Tensho] \"+text);"),"local chat line only");
        }),
        // From HuntBills440Tests: N-01 (0.440): held hunt bills with their targets, kills and zone.
        ("Hunt bills: targets of the held order, kills clamped, done when all are; unknown orders left out",() =>
        {
            // Order 1 of docs/evidence/0.440-hunt-bills.md (five targets, the last two elite FATE marks).
            IReadOnlyList<HuntOrderTarget> Order(uint row)=>row==1
                ? [new(0,"Géant bûcheron",53,"Coteaux rocheux",3,0),new(1,"Hoplomachus",25,"Fangebrume",3,0),new(2,"Épine",18,"Frai de Sapsa",3,0),
                   new(3,"Faucheux",20,"Vallée des Pas perdus",1,366),new(4,"",0,"",0,0)]
                : [];
            var views=HuntBills.Views([new(0,1,[3,1,5,0,0]),new(4,999,[1,0,0,0,0])],Order,index=>$"Bill {index}");
            Check(views.Count==1 && views[0].Bill=="Bill 0" && views[0].Targets.Count==4,"one known bill, empty slot dropped");
            Check(views[0].Targets[0].Done && !views[0].Targets[1].Done && views[0].Targets[2].Killed==3 && !views[0].Done,"kills clamped to the need");
            var done=HuntBills.Views([new(0,1,[3,3,3,1,0])],Order,_=>"B");
            Check(done.Single().Done,"all targets: done");
            Check(HuntBills.Views(null,Order,_=>"").Count==0,"nothing read");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs"));
            Check(reader.Contains("(nint)MobHunt.MemberFunctionPointers.GetObtainedHuntOrderRowId!=0"),"address checked");
        }),
        // From HuntMarks441Tests: N-09 (0.441): living hunt marks on the Tensho map, by rank.
        ("Hunt marks: NotoriousMonster ranks 1-3, living and targetable only, own layer and colours",() =>
        {
            var reader=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("mark.Rank is >= 1 and <= 3)huntMarks.TryAdd(mark.BNpcBase.RowId,mark.Rank)"),"ranks from the sheet");
            Check(reader.Contains("IBattleNpc { CurrentHp:>0 }=>MapObjectKind.HuntMark") && reader.Contains("if(kind is null || !item.IsTargetable)continue;"),"alive, targetable");
            var view=Read("MainWindow.MapView.cs");
            Check(view.Contains("MapObjectKind.HuntMark=>!settings.ShowHuntMarks") && view.Contains("MapView.HuntMarkLabel"),"layer setting and label");
            Check(new TenshoAssistant.MapViewSettings().ShowHuntMarks,"on by default");
        }),
        // From HuntTrain442Tests: N-15 (0.442): hunt train recorder — once per map, next stop, export read back by the bookmark import.
        ("Hunt train: each mark once per map, next stop, export read back as bookmarks",() =>
        {
            var at=DateTimeOffset.UnixEpoch;
            IReadOnlyList<HuntTrainStop> stops=[];
            stops=HuntTrain.Add(stops,new("Forneus",2,20,140,"Thanalan occidental",12.3f,7.4f,at));
            stops=HuntTrain.Add(stops,new("forneus",2,20,140,"Thanalan occidental",13f,8f,at));
            stops=HuntTrain.Add(stops,new("Laideronnette",3,30,150,"Kozama'uka",30.1f,15.2f,at));
            Check(stops.Count==2,"same name and map: once");
            Check(HuntTrain.Next(stops)==0 && HuntTrain.Next(HuntTrain.MarkVisited(stops,0))==1 && HuntTrain.Next(HuntTrain.MarkVisited(HuntTrain.MarkVisited(stops,0),1)) is null,"next stop");
            var text=HuntTrain.Export(stops);
            Check(text=="Forneus — Thanalan occidental ( 12.3 , 7.4 )\nLaideronnette — Kozama'uka ( 30.1 , 15.2 )","export lines");
            var zones=new Dictionary<string,MapDefinition>{["Thanalan occidental"]=new(20,140,"","Thanalan occidental","",0,100),["Kozama'uka"]=new(30,150,"","Kozama'uka","",0,100)};
            var back=MapBookmarks.ParseCoordinateList(text,name=>zones.GetValueOrDefault(name),"Train");
            Check(back.Bookmarks.Count==2 && back.Unknown==0 && back.Bookmarks[1].X==30.1f,"read back by the bookmark import");
            var full=Enumerable.Range(0,70).Aggregate((IReadOnlyList<HuntTrainStop>)[],(list,index)=>HuntTrain.Add(list,new($"M{index}",2,1,1,"Z",1,1,at)));
            Check(full.Count==HuntTrain.Capacity,"bounded");
        }),
        // From HuntingLog364Tests: 0.364 (g, C-12): hunting log — row layout, memory slots, kills of earlier, current and later ranks.
        ("Hunting log: rows split into log, rank and number; passed ranks complete, later ranks zero, current rank from memory",() =>
        {
            var entry = new HuntEntry(10023,"Gladiateur 23",[new HuntTarget("bombo",3,"Thanalan"),new HuntTarget("coblyn",2,"Thanalan")]);
            Check(entry.LogKey == 1 && entry.Rank == 2 && entry.Number == 2 && new HuntEntry(1000001,"GC",[]).LogKey == 100,"row layout");
            Check(HuntingLog.MemorySlot(1) == 0 && HuntingLog.MemorySlot(26) == 7 && HuntingLog.MemorySlot(100) == 8 && HuntingLog.MemorySlot(300) == 10
                && HuntingLog.MemorySlot(29) == 11 && HuntingLog.MemorySlot(8) is null,"memory slots");
            IReadOnlyList<IReadOnlyList<byte>> counts = Enumerable.Range(0,10).Select(i => (IReadOnlyList<byte>)(i == 2 ? new byte[]{3,1,0,0} : new byte[4])).ToArray();
            Check(HuntingLog.Killed(entry,0,new HuntSlot(3,counts)) == 3 && HuntingLog.Done(entry,new HuntSlot(3,counts)) == true,"passed rank: done");
            Check(HuntingLog.Killed(entry,1,new HuntSlot(1,counts)) == 0 && HuntingLog.Done(entry,new HuntSlot(1,counts)) == false,"later rank: zero");
            Check(HuntingLog.Killed(entry,0,new HuntSlot(2,counts)) == 3 && HuntingLog.Killed(entry,1,new HuntSlot(2,counts)) == 1
                && HuntingLog.Done(entry,new HuntSlot(2,counts)) == false && HuntingLog.Done(entry,null) is null,"current rank from memory; not read = unknown");
            Check(Read("Hunting/HuntingLogReader.Dalamud.cs").Contains("MonsterNoteManager.Instance()",StringComparison.Ordinal)
                && Read("Plugin.cs").Contains("()=>huntReader?.Dispose()",StringComparison.Ordinal),"reader guarded and disposed");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.364-hunting-log.md")),"evidence");
        }),
        // From HuntingLogCompany393Tests: 0.393 (user reports): the hunting log shows only the character's own Grand Company log; French name « Bestiaire » (not « chasse »).
        ("Hunting log: own Grand Company only, none without one, all while unknown; class logs always; French name Bestiaire",() =>
        {
            Check(HuntingLog.Shown(100,1) && !HuntingLog.Shown(200,1) && !HuntingLog.Shown(300,1),"Maelstrom only");
            Check(!HuntingLog.Shown(100,0) && HuntingLog.Shown(1,0),"no company: class logs only");
            Check(HuntingLog.Shown(200,null) && HuntingLog.Shown(26,3),"unknown: all; class logs always");
            var french = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(french.Contains("\"HuntingLog\": \"Bestiaire\"",StringComparison.Ordinal) && !french.Contains("Carnet de chasse",StringComparison.Ordinal),"Bestiaire");
        }),
    ];
}
