using TenshoAssistant.Maps;

// 0.1320 (X-322, user report of 2026-11-14 at Mor Dhona): the icon « Exchange » alone at X 21.4 Y 5.5 is the fixed map icon MapMarker 108.46
// (icon 60935, unlock bit 44), drawn by the game only once unlocked (docs/evidence/0.1320-map-marker-unlock.md). The rule, the game's list
// position, the comparison the self-check makes, and the wiring.
internal static class MapMarkerUnlock1320Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map marker unlock 1320: no bit always drawn; a bit drawn only when read unlocked; unknown hides it",() =>
        {
            Check(MapMarkerUnlock.Visible(0,null) && MapMarkerUnlock.Visible(0,new HashSet<ushort>()),"no bit: always");
            Check(!MapMarkerUnlock.Visible(44,new HashSet<ushort>{43}),"Mor Dhona's 108.46: bit 44 not unlocked, hidden");
            Check(MapMarkerUnlock.Visible(43,new HashSet<ushort>{43}),"108.45: bit 43 unlocked, drawn");
            Check(!MapMarkerUnlock.Visible(44,null),"not read (outside the game): hidden, never shown as there");
        }),
        ("Map marker unlock 1320: the game's list position (world × 16) of a sheet pixel, checked on Mor Dhona's aetheryte",() =>
        {
            Check(MapMarkerUnlock.GamePosition(1064,356,100,0,0)==(640f,-10688f),"108.3 is listed at 640, -10688 (dalamud.log)");
            var (x,y)=MapMarkerUnlock.GamePosition(1024+100,1024-200,200,-50,10);
            // Size factor 200, offsets −50 and 10: world (100/2 + 50, −200/2 − 10) = (100, −110), × 16.
            Check(x==1600f && y==-1760f,"scale and offset reversed as PixelFromWorld: "+x+","+y);
        }),
        ("Map marker unlock 1320: the self-check's comparison: listed or not against the bit, flags kept, rows without a bit left out",() =>
        {
            var sheet=new (ushort,uint,float,float)[]{(0,60453,1064,356),(43,60935,1082,244),(44,60935,1019,225)};
            var (x43,y43)=MapMarkerUnlock.GamePosition(1082,244,100,0,0);
            var game=new List<(uint,int,int,int)>{(60453,640,-10688,0x4002),(60935,(int)x43+5,(int)y43-3,0x0002)};
            var checks=MapMarkerUnlock.Compare(sheet,100,0,0,game,new HashSet<ushort>{43});
            Check(checks.Count==2,"only the rows with a bit: "+checks.Count);
            Check(checks[0] is { Bit:43, Listed:true, Flags:2, Unlocked:true } && checks[1] is { Bit:44, Listed:false, Flags:-1, Unlocked:false },"agree: "+string.Join("; ",checks.Select(MapMarkerUnlock.Describe)));
            Check(MapMarkerUnlock.Describe(checks[1])=="bit 44 icon 60935 at 1019,225: listed no, unlocked no","the log's words");
            var wrong=MapMarkerUnlock.Compare(sheet,100,0,0,game,new HashSet<ushort>{44});
            Check(wrong.Any(check=>check.Listed!=check.Unlocked),"a wrong reading of the bits shows as a disagreement");
            var far=new List<(uint,int,int,int)>{(60935,(int)x43+40,(int)y43,0x0002)};
            Check(!MapMarkerUnlock.Compare(sheet,100,0,0,far,new HashSet<ushort>())[0].Listed,"more than two world units away: not the same icon");
        }),
        ("Map marker unlock 1320: the sheet's bit is read, the map and the vendor list draw only the visible icons, the game is checked",() =>
        {
            var source=Read("src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs");
            Check(source.Contains("string Title=\"\",ushort UnlockBit=0)") && source.Contains("AetheryteName(row.DataKey.RowId):\"\",row.MapMarkerUnlockedBit));"),"UnlockBit from MapMarkerUnlockedBit");
            var view=Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(view.Contains("? VisibleFixed(mapMarkers?.For(map.MarkerRange) ?? []):[];") && view.Contains("MapMarkerUnlock.Visible(marker.UnlockBit,unlocked)"),"the map draws the visible icons");
            Check(view.Contains("if(fixedShown is { } known && ReferenceEquals(known.From,all) && ReferenceEquals(known.Unlocked,unlocked))return known.Shown;"),"kept, not filtered in every frame");
            Check(Read("src/TenshoAssistant/MainWindow.MapLocalLists.cs").Contains("VendorCandidates(VisibleFixed(mapMarkers?.For(map.MarkerRange) ?? []))"),"the vendor list too");
            var reader=Read("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("if(ui==null || (nint)UIState.MemberFunctionPointers.IsUnlockLinkUnlocked==0)return null;") && reader.Contains("before.SetEquals(set) ? before:set"),"read guarded, same set kept");
            Check(reader.Contains("try{unlockedMarkerBits=ReadMarkerUnlocks();CheckMarkerUnlocks();Read(\"map icon unlocks\");}catch(Exception error){unlockedMarkerBits=null;Failed(\"map icon unlocks\",error);}"),"in the two-second reads, a failure hides");
            Check(Read("src/TenshoAssistant/MainWindow.SelfCheckPages.cs").Contains("Page(\"map icon unlocks\""),"self-check");
        }),
    ];
}
