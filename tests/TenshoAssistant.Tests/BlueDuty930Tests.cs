using TenshoAssistant.Collections;

// X-76 P5 (0.930): a blue-magic spell learned in a duty shows the duty's level, read from ContentFinderCondition.ClassJobLevelRequired.
internal static class BlueDuty930Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Blue duty 930: duty level kept on the entry, read from the duty row, shown after the place",() =>
        {
            Check(new CollectionEntry(CollectionKind.BlueMagic,1,"S",0,false,DutyLevel:50).DutyLevel==50 && new CollectionEntry(CollectionKind.Mount,1,"M",0,true).DutyLevel==0,"field");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Collections/CollectionReader.Dalamud.cs")).Contains("dutyLevel = content.ClassJobLevelRequired;"),"read");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Collections.cs")).Contains("localization.Format(\"BlueSpell.DutyLevel\",spell.DutyLevel)"),"shown");
        }),
    ];
}
