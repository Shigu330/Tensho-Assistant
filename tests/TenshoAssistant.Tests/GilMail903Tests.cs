// 0.903 (X-84): gil taken from or attached to a letter are filed under « Mail » in the gil journal.
using TenshoAssistant.Inventory;
internal static class GilMail903Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil journal: the letter window gives « Mail », added last so saved numbers stay; not a transfer",() =>
        {
            var now=DateTimeOffset.UtcNow;
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>{["LetterList"]=now,["LetterViewer"]=now.AddSeconds(-2)},now)==GilCategory.Mail,"viewer");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>{["LetterEditor"]=now},now)==GilCategory.Mail,"editor");
            Check((int)GilCategory.Mail==7 && (int)GilCategory.Company==6 && !GilJournal.IsTransfer(GilCategory.Mail),"numbers, not a transfer");
        }),
    ];
}
