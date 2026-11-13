using TenshoAssistant.Collections;

// 0.1229 (X-219, user's request of 12/11, K-34): the mount tooltip of Collections says how many riders the mount carries, whether its riding music
// is its own, shared or absent, and its mount actions, from the Mount sheet (docs/evidence/0.1229-mount-sheet.md). The values come from the game
// sheet at run time; these tests check the wiring, the texts in both languages and that nothing is guessed when the sheet does not read.
internal static class MountSheet1229Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Mount sheet 1229: the entry carries riders, music sharing and actions; absent by default",() =>
        {
            var entry=new CollectionEntry(CollectionKind.Mount,1,"a",0,false);
            Check(entry.Mount is null,"no mount details unless read");
            var read=entry with {Mount=new MountMetadata(8,1,["Sneeze"])};
            Check(read.Mount!.Riders==8 && read.Mount.MusicShared==1 && read.Mount.Actions.Single()=="Sneeze","details kept");
        }),
        ("Mount sheet 1229: the reader fills it from ExtraSeats, RideBGM and MountAction, null when a column does not read",() =>
        {
            var reader=Source("src/TenshoAssistant/Collections/CollectionReader.Dalamud.cs");
            Check(reader.Contains("Mount:MountDetails(row)") && reader.Contains("return new(row.ExtraSeats+1,music,actions);")
                  && reader.Contains("row.RideBGM.RowId==0 ? 0:mountMusic.GetValueOrDefault(row.RideBGM.RowId)")
                  && reader.Contains("catch(Exception error) when(error is not OutOfMemoryException){return null;}"),"reader wiring");
            Check(reader.Contains("docs/evidence/0.1229-mount-sheet.md") && File.Exists(Path.GetFullPath("docs/evidence/0.1229-mount-sheet.md")),"evidence cited and present");
        }),
        ("Mount sheet 1229: the tooltip lines exist in English and French",() =>
        {
            var hover=Source("src/TenshoAssistant/MainWindow.MarketHover.cs");
            Check(hover.Contains("if(entry.Mount is { } mount)") && hover.Contains("\"HoverUi.MountSharedMusic\",shared-1"),"tooltip wiring");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Source($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach(var key in new[]{"MountRider","MountRiders","MountOwnMusic","MountSharedMusic","MountNoMusic","MountActions"})
                    Check(text.Contains($"\"{key}\": ",StringComparison.Ordinal),language+": "+key);
            }
        }),
    ];
}
