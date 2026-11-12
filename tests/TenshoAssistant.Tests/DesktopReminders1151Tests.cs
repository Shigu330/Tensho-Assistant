using TenshoAssistant.Character;
using TenshoAssistant.Desktop;
using TenshoAssistant.Duties;
using TenshoAssistant.Link;

// 0.1151 (X-119): the player's characters (Reminders › Characters) and the duty progress (Duties, Deep dungeons) in the desktop application, read
// from its own copy of Characters/ and DutyProgress.json. Behaviour tests on real files: the plugin's files copied by DesktopMirror, read as the
// application does (a damaged character file left out and said, the plugin's file never touched); the duty state of the last character, as the game
// shows it logged out; a damaged duty file is unknown, never a guess.
internal static class DesktopReminders1151Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026,11,11,20,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string,string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-rem-1151-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(Path.Combine(root,"plugin"),Path.Combine(root,"mirror")); }
        finally { try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop reminders 1151: the characters copied and read as the application does; a damaged file left out and said, the plugin's files untouched",() => InTemp((plugin,mirror) =>
        {
            var folder = Path.Combine(plugin,"Characters");
            Directory.CreateDirectory(folder);
            CharacterStatuses.Write(folder,new CharacterStatus { ContentId = 0x8000_0000_0000_0011UL,Name = "Alpha Beta",SeenAt = At,HuntBills = 3,NextMap = At.AddHours(5) });
            CharacterStatuses.Write(folder,new CharacterStatus { ContentId = 22,Name = "Gamma Delta",SeenAt = At.AddHours(1),RoulettesDone = 2,RoulettesTotal = 5,RoulettesAt = At });
            File.WriteAllText(Path.Combine(folder,"33.json"),"{ damaged");
            var before = Directory.GetFiles(folder).OrderBy(path => path).Select(File.ReadAllBytes).ToArray();
            var copy = DesktopMirror.Copy(plugin,mirror,"characters");
            Check(copy.Copied == 3 && copy.Problems.Count == 0,"three files copied: " + string.Join("; ",copy.Problems));
            var failed = new List<string>();
            var read = CharacterStatuses.ReadAll(Path.Combine(mirror,"Characters"),(path,_) => failed.Add(Path.GetFileName(path)));
            Check(read.Count == 2 && read.Any(status => status.ContentId == 0x8000_0000_0000_0011UL && status.HuntBills == 3 && status.NextMap == At.AddHours(5))
                && read.Any(status => status.ContentId == 22 && status.RoulettesDone == 2),"both characters as the plugin saved them");
            Check(failed.SequenceEqual(["33.json"]),"the damaged file said, left out");
            Check(Directory.GetFiles(folder).OrderBy(path => path).Select(File.ReadAllBytes).Zip(before).All(pair => pair.First.SequenceEqual(pair.Second)),"the plugin's files untouched");
            File.Delete(Path.Combine(folder,"22.json"));
            DesktopMirror.Copy(plugin,mirror,"characters");
            Check(CharacterStatuses.ReadAll(Path.Combine(mirror,"Characters"),(_,_) => { }).Select(status => status.ContentId).SequenceEqual([0x8000_0000_0000_0011UL]),"a character erased in the game goes from the copy");
        })),
        ("Desktop reminders 1151: the duty progress of the last character, as the game shows it logged out; a damaged file is unknown",() => InTemp((plugin,mirror) =>
        {
            Directory.CreateDirectory(plugin);
            var all = new Dictionary<ulong,DutyProgress.Saved>
            {
                [5] = new(new() { [1] = DutyStatus.Completed,[2] = DutyStatus.Unlocked },At),
                [6] = new(new() { [1] = DutyStatus.Unlocked,[3] = DutyStatus.Locked },At.AddHours(2)),
            };
            File.WriteAllText(Path.Combine(plugin,"DutyProgress.json"),DutyProgress.Write(all,6));
            Check(DesktopMirror.Copy(plugin,mirror,"duties").Copied == 1,"copied");
            var state = DutyProgress.LastShown(File.ReadAllText(Path.Combine(mirror,"DutyProgress.json")));
            Check(state is { FromDisk: true } && state.ReadAt == At.AddHours(2) && state.Of(1) == DutyStatus.Unlocked && state.Of(3) == DutyStatus.Locked && state.Of(2) is null,
                "the last character's state, an unknown duty stays unknown");
            Check(DutyProgress.LastShown(DutyProgress.Write(all,9)) is null,"no state for the last character: none shown");
            var damaged = false;
            try { DutyProgress.LastShown("{ damaged"); } catch (InvalidDataException) { damaged = true; }
            Check(damaged,"a damaged file is said, never read as « nothing done »");
        })),
        ("Desktop reminders 1151: the categories, the reading off the frame, the newest reading kept, no erase button in the application",() =>
        {
            Check(LocalLinkProtocol.DesktopCategories.Contains("characters") && LocalLinkProtocol.DesktopCategories.Contains("duties"),"categories read by the application");
            var data = Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(data.Contains("if(Volatile.Read(ref desktopCharactersGeneration)==generation)Volatile.Write(ref desktopCharacters,read);",StringComparison.Ordinal)
                && data.Contains("if(Volatile.Read(ref desktopDutiesGeneration)==generation)Volatile.Write(ref desktopDuties,state);",StringComparison.Ordinal)
                && data.Contains("DutyProgressSource=()=>Volatile.Read(ref desktopDuties);",StringComparison.Ordinal)
                && data.Contains("try{if(File.Exists(file))state=Duties.DutyProgress.LastShown(File.ReadAllText(file));}",StringComparison.Ordinal),"desktop data");
            var page = Read("src/TenshoAssistant/MainWindow.CharacterStatus.cs");
            Check(page.Contains("else if(Volatile.Read(ref desktopCharacters) is { } copy)all=[.. copy.OrderBy(status=>status.Name)];",StringComparison.Ordinal)
                && page.Contains("if(source is not null && status.ContentId!=current)",StringComparison.Ordinal),"the page reads the copy, erasing only in the game");
        }),
    ];
}
