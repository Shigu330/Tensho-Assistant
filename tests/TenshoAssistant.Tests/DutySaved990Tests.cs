using TenshoAssistant.Duties;

// X-106 (0.990): the duties' completion kept per character on disk, shown logged out as « saved ».
internal static class DutySaved990Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Duty saved 990: round trip per character with the last one, broken file read as none",() =>
        {
            var at=DateTimeOffset.UtcNow;
            var all=new Dictionary<ulong,DutyProgress.Saved>{[7]=new(new(){[1]=DutyStatus.Completed,[2]=DutyStatus.Unlocked},at)};
            Check(DutyProgress.Read(DutyProgress.Write(all,7)) is { } back && back.Last==7 && back.All[7].ByInstance[1]==DutyStatus.Completed,"round trip");
            Check(DutyProgress.Read("{x")==null,"broken");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Duties/DutyProgressReader.Dalamud.cs"));
            Check(reader.Contains("writer.Enqueue(\"Duty progress could not be saved.\"") && reader.Contains("new DutyProgressState(last.ByInstance,last.ReadAt,true)"),"saved, shown");
        }),
    ];
}
