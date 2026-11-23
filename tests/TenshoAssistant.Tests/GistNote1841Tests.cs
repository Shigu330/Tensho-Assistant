// 0.1841 (X-837 M1, review of 0.1827 to 0.1830): for a gist with nothing new, sync said « no write » and then noted the gist as merged, a
// change of the repository: a worker reading the first line opened no pull request and the gist (exact places and days) stayed online. The
// line now says that the note is a change that needs its pull request.
internal static class GistNote1841Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    public static readonly (string Name,Action Run)[] Cases=
    [
        ("X-837 M1: sync no longer says « no write » for a gist it notes as merged; it says the note needs its pull request",()=>
        {
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(!tool.Contains("no write, no « Merged » comment",StringComparison.Ordinal),"the old line is gone");
            Check(tool.Contains("its gist is still noted as merged, a change that needs its pull request",StringComparison.Ordinal),"the note said as a change");
            Check(tool.Contains("if (added,gone)==(0,0):say(f\"#{issue['number']}: its gist noted as merged\");continue",StringComparison.Ordinal),"the note itself unchanged");
        }),
    ];
}
