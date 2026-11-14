using System.Text.RegularExpressions;
using TenshoAssistant.Inventory;

// 0.1334 (X-350, review of the block 0.1318 to 0.1329, I1): R03.2 and DEC-038: X-320's chat.LogMessage subscriber (gil journal) did not check
// CompetitiveGate.Cut, the two other subscribers did; in PvP, ranked content, Triple Triad or Mahjong, Tensho kept noting messages.
internal static class ChatCut1334Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now=new(2026,11,14,15,40,0,TimeSpan.Zero);
    private static string Root=>Path.GetFullPath("src/TenshoAssistant");
    private static IEnumerable<string> Code()=>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
        .Where(file=>!file.Contains("\\obj\\") && !file.Contains("/obj/") && !file.Contains("\\bin\\") && !file.Contains("/bin/"));
    private static readonly Regex Subscribed=new(@"\.(LogMessage|ChatMessage|ChatMessageHandled|ChatMessageUnhandled|CheckMessageHandled)\s*\+=\s*",RegexOptions.Compiled);
    private static readonly Regex Named=new(@"\G([A-Za-z_]\w*)\s*[;,)]",RegexOptions.Compiled);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat cut 1334: a gil message is noted outside the cut, never during it",() =>
        {
            var seen=new Dictionary<uint,DateTimeOffset>();
            Check(!GilJournal.NoteMessage(seen,3780,Now,cut:true) && !GilJournal.NoteMessage(seen,557,Now,cut:true) && seen.Count==0,"cut: nothing noted");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>(),Now,seen)==GilCategory.Other,"so a gain right after is not taken for a challenge log entry");
            Check(GilJournal.NoteMessage(seen,3780,Now,cut:false) && seen[3780]==Now,"outside the cut: noted");
            Check(!GilJournal.NoteMessage(seen,1,Now,cut:false) && seen.Count==1,"another message: not noted");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>(),Now,seen)==GilCategory.ChallengeLog,"the noted message still classifies the gain");
        }),
        ("Chat cut 1334: ratchet: every chat subscriber is a named method that checks CompetitiveGate.Cut",() =>
        {
            var found=0;
            foreach(var file in Code())
            {
                var text=File.ReadAllText(file).Replace("\r\n","\n");
                foreach(Match match in Subscribed.Matches(text))
                {
                    var name=Named.Match(text,match.Index+match.Length);
                    var where=Path.GetRelativePath(Root,file)+": "+match.Value.Trim();
                    Check(name.Success,"a chat subscriber is a named method (a lambda cannot be checked): "+where);
                    var handler=Regex.Match(text,@"void "+Regex.Escape(name.Groups[1].Value)+@"\([^)]*\)\s*\n\s*\{(.*?)\n    \}",RegexOptions.Singleline);
                    Check(handler.Success,"handler "+name.Groups[1].Value+" found in "+where);
                    Check(handler.Groups[1].Value.Contains("CompetitiveGate.Cut",StringComparison.Ordinal),"handler "+name.Groups[1].Value+" checks the cut (R03.2): "+where);
                    found++;
                }
            }
            Check(found>=3,"the three chat subscribers seen (discoveries, S rank kills, gil journal): "+found);
        }),
    ];
}
