// 0.1870 (X-864, review 0.1868 M2): the ratchet of 0.1868 saw only « .Contains(…) ». A raw read searched by IndexOf, LastIndexOf,
// StartsWith, EndsWith or Split with a « …x\n… » snippet escaped it (an absence check « IndexOf(…)<0 » is then always true on the CRLF
// runner), and so did a read chained before its search (« File.ReadAllText(…).Trim().Contains(…) ») and a helper of another file. These
// cases hold the forms it now finds, and the ones it still lets pass.
internal static class RawNewline1870Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static int Count(string source)=>Pending1865Tests.RawNewlineSearches(source).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raw newline 1870: IndexOf, LastIndexOf, StartsWith, EndsWith and Split with a « …x\\n… » snippet are found, in line and through a variable",() =>
        {
            foreach(var search in new[]{"IndexOf","LastIndexOf","StartsWith","EndsWith","Split"})
            {
                Check(Count("Check(File.ReadAllText(\"a\")."+search+"(\"a;\\n b\")>=0,\"x\");\n")==1,search+" in line");
                Check(Count("var s=File.ReadAllText(\"a\");\nCheck(s."+search+"(\"a;\\n b\")<0,\"x\");\n")==1,search+" through a variable");
                Check(Count("var s=File.ReadAllText(\"a\");\nCheck(s."+search+"(\"a;\\n b\")<0 && s."+search+"(\"a;\\r\\n b\")<0,\"x\");\n")==0,search+" taking both forms passes");
            }
            // X-866 (0.1871, review 0.1870 M1): a split on « \n » alone leaves a « \r » at the end of each piece of a CRLF text: found.
            Check(Count("var s=File.ReadAllText(\"a\");\nvar lines=s.Split(\"\\n\");\n")==1,"a split on « \\n » alone is found (it leaves a « \\r » in each piece)");
        }),
        ("Raw newline 1870: a read chained before its search is found; a normalisation anywhere in the chain lets it pass",() =>
        {
            Check(Count("Check(File.ReadAllText(\"a\").Trim().Contains(\"a;\\n b\"),\"x\");\n")==1,"read, Trim, search in line");
            Check(Count("Check(File.ReadAllText(Path.Combine(\"a\",\"b\")).Substring(3).ToLowerInvariant().IndexOf(\"a;\\n b\")<0,\"x\");\n")==1,"several calls");
            Check(Count("var s=File.ReadAllText(\"a\").Trim();\nCheck(s.Contains(\"a;\\n b\"),\"x\");\n")==1,"chained read kept in a variable");
            Check(Count("var s=File.ReadAllText(\"a\");\nCheck(s.Trim().Contains(\"a;\\n b\"),\"x\");\n")==1,"variable chained before its search");
            Check(Count("static string Read(string f)=>File.ReadAllText(f).Trim();\nvoid T(){Check(Read(\"a\").Contains(\"a;\\n b\"),\"x\");}\n")==1,"helper with a chained read");
            Check(Count("Check(File.ReadAllText(\"a\").Trim().ReplaceLineEndings(\"\\n\").Contains(\"a;\\n b\"),\"x\");\n")==0
                && Count("var s=File.ReadAllText(\"a\").Trim().Replace(\"\\r\\n\",\"\\n\");\nCheck(s.IndexOf(\"a;\\n b\")<0,\"x\");\n")==0
                && Count("var s=File.ReadAllText(\"a\");\nCheck(s.ReplaceLineEndings(\"\\n\").Contains(\"a;\\n b\"),\"x\");\n")==0
                && Count("static string Read(string f)=>File.ReadAllText(f).Trim().ReplaceLineEndings(\"\\n\");\nvoid T(){Check(Read(\"a\").Contains(\"a;\\n b\"),\"x\");}\n")==0,
                "a normalisation in the chain, on the variable, or in the helper passes");
        }),
        ("Raw newline 1870: a shared helper of another file is found from the files that call it; a private one is never offered",() =>
        {
            var helpers=Pending1865Tests.RawHelpers("internal static class Texts\n{\n    internal static string Source(string f)=>File.ReadAllText(f);\n"
                +"    private static string Hidden(string f)=>File.ReadAllText(f);\n    internal static string Clean(string f)=>File.ReadAllText(f).ReplaceLineEndings(\"\\n\");\n}\n");
            Check(helpers.Count==2 && helpers.All(helper=>helper.Class=="Texts"),"two raw helpers in class Texts: "+string.Join(", ",helpers));
            Check(helpers.Single(helper=>helper.Name=="Source").Shared && !helpers.Single(helper=>helper.Name=="Hidden").Shared,"internal is shared, private is not");
            var caller="void T(){Check(Texts.Source(\"a\").Contains(\"a;\\n b\"),\"x\");\nvar s=Texts.Source(\"b\");\nCheck(s.EndsWith(\"}\\n\\n\"),\"x\");}\n";
            Check(Pending1865Tests.RawNewlineSearches(caller,[("Texts.Source",false)]).Count==2,"found in line and through a variable from another file");
            Check(Pending1865Tests.RawNewlineSearches(caller).Count==0,"not found without the helper (the file alone cannot tell)");
        }),
    ];
}
