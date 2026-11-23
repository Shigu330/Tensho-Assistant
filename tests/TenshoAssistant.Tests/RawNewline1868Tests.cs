// 0.1868 (X-861, review 0.1866 M1): the ratchet of 0.1865 saw only « var x=File.ReadAllText(…); then x.Contains("…\n…") », and any Replace
// among the read's arguments let it pass. These cases hold the forms it now finds, and the ones it lets pass.
internal static class RawNewline1868Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static int Count(string source)=>Pending1865Tests.RawNewlineSearches(source).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raw newline 1868: the ratchet finds a raw read searched in line, through « string x= », and through a helper of the file",() =>
        {
            Check(Count("Check(File.ReadAllText(Path.GetFullPath(\"a.py\")).Contains(\"a:\\n b\"),\"x\");\n")==1,"in line");
            Check(Count("string tool=File.ReadAllText(\"a.py\");\nCheck(tool.Contains(\"a:\\n b\"),\"x\");\n")==1,"string x=");
            Check(Count("static string Read=>File.ReadAllText(Path.GetFullPath(path));\nvoid T(){Check(Read.Contains(\"a:\\n b\"),\"x\");}\n")==1,"helper property, in line");
            Check(Count("static string Read(string f)=>File.ReadAllText(Path.Combine(\"src\",f));\nvoid T(){var s=Read(\"a.cs\");\nCheck(s.Contains(\"a;\\n b\"),\"x\");}\n")==1,
                "helper method, through a variable");
            Check(Count("static string Read(string f){return File.ReadAllText(f);}\nvoid T(){Check(Read(\"a\").Contains(\"a\\n\"),\"x\");}\n")==1,"helper with a body");
        }),
        ("Raw newline 1868: a Replace among the arguments no longer lets a raw read pass; a normalisation right after the read does",() =>
        {
            Check(Count("var tool=File.ReadAllText(Path.GetFullPath(\"a.py\".Replace('/','\\\\')));\nCheck(tool.Contains(\"a:\\n b\"),\"x\");\n")==1,
                "a Replace of the path is not a normalisation");
            Check(Count("var a=File.ReadAllText(\"a\").ReplaceLineEndings(\"\\n\");\nCheck(a.Contains(\"a\\n\"),\"x\");\n")==0
                && Count("var a=File.ReadAllText(\"a\").Replace(\"\\r\\n\",\"\\n\");\nCheck(a.Contains(\"a\\n\"),\"x\");\n")==0
                && Count("static string Read=>File.ReadAllText(p).Replace(\"\\r\\n\",\"\\n\");\nvoid T(){Check(Read.Contains(\"a\\n\"),\"x\");}\n")==0
                && Count("Check(File.ReadAllText(\"a\").Contains(\"a\\n b\")\n    || File.ReadAllText(\"a\").Contains(\"a\\r\\n b\"),\"x\");\n")==0,
                "normalised reads, a normalising helper, and a search that takes both forms pass");
            Check(Count("Check(File.ReadAllText(\"a\").Contains(\"plain\"),\"x\");\n")==0,"a snippet without « \\n » passes");
            Check(Count("Check(File.ReadAllText(\"a.json\").Contains(\"\\n    \\\"Rule\\\": \\\"\"),\"x\");\n")==0,
                "a snippet that starts with « \\n » passes (« \\r\\n » holds it too)");
            Check(Count("Check(File.ReadAllText(\"a\").Contains(\"\\n\\n\"),\"x\");\n")==1,"« \\n\\n » is found (« \\r\\n\\r\\n » does not hold it)");
        }),
    ];
}
