// 0.1871 (X-866, review 0.1870 M1 and M2): the ratchet of the raw newline searches (Pending1865Tests). M1: the exemption « a « \n » that
// starts the snippet is also in « \r\n » » holds for Contains, IndexOf, LastIndexOf and EndsWith only; StartsWith and Split are found
// with any « \n » (string or character; for a split, since 0.1872, X-868, any of the separators written in the call: several, an
// array or a collection expression), and a split passes only when it takes the « \r » into account (since 0.1872, X-867, from the
// split to the end of its statement: see RawNewline1872Tests and RawNewline1873Tests). M2: the class of a raw helper
// is the type whose body holds it, found in the code only (a « class » in a comment or a string no longer names it), records and
// structs included. Neighbour cases written here so the next review finds them settled: Replace, the character forms, a read written in
// a comment or a string, verbatim and raw literals. Not covered on purpose (reasons in Pending1865Tests: NewlineSearch, Handled and
// RawHelpers): Regex calls, « == » or Equals against a whole read, reads other than File.ReadAllText, a nested helper called by its
// short name, split separators held in a variable or a field, a Trim or a « \r » on another expression of the split's statement.
internal static class RawNewline1871Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static int Count(string source)=>Pending1865Tests.RawNewlineSearches(source).Count;

    private static string Owner(string source)=>Pending1865Tests.RawHelpers(source).Single().Class;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raw newline 1871: StartsWith and Split are found with a « \\n » anywhere, string or character; the other searches keep the leading « \\n » exemption",() =>
        {
            Check(Count("var s=File.ReadAllText(\"a\");\nCheck(s.StartsWith(\"\\nX\"),\"x\");\n")==1,"StartsWith with a leading « \\n »");
            Check(Count("Check(File.ReadAllText(\"a\").StartsWith('\\n'),\"x\");\n")==1,"StartsWith with the character « \\n »");
            Check(Count("var lines=File.ReadAllText(\"a\").Split('\\n');\nCheck(!lines.Contains(\"x\"),\"x\");\n")==1,"Split on the character « \\n », in line");
            Check(Count("var s=File.ReadAllText(\"a\");\nvar lines=s.Split('\\n');\nCheck(lines[0]==\"x\",\"x\");\n")==1,"Split on the character « \\n », through a variable");
            Check(Count("var s=File.ReadAllText(\"a\");\nCheck(s.StartsWith(\"\\nX\") || s.StartsWith(\"\\r\\nX\"),\"x\");\n")==0,"StartsWith taking both forms passes");
            foreach(var search in new[]{"Contains","IndexOf","LastIndexOf","EndsWith"})
                Check(Count("var s=File.ReadAllText(\"a\");\nCheck(s."+search+"(\"\\nX\")>=0,\"x\");\n")==0,search+" with a leading « \\n » still passes");
        }),
        ("Raw newline 1871: a split passes when it takes the « \\r » into account: « \\r » among the separators or trimmed, or each piece trimmed",() =>
        {
            Check(Count("var lines=File.ReadAllText(\"a\").Split(new[]{\"\\r\\n\",\"\\n\"},StringSplitOptions.None);\n")==0,"« \\r\\n » among the separators");
            Check(Count("var lines=File.ReadAllText(\"a\").Split('\\n').Select(line=>line.TrimEnd('\\r'));\n")==0,"« \\r » trimmed from each piece");
            Check(Count("var lines=File.ReadAllText(\"a\").Split('\\n',StringSplitOptions.TrimEntries);\n")==0,"TrimEntries");
            Check(Count("var s=File.ReadAllText(\"a\");\nforeach(var line in s.Split('\\n').Select(line=>line.Trim()))Check(line!=\"x\",\"x\");\n")==0,"each piece trimmed");
            Check(Count("var s=File.ReadAllText(\"a\").ReplaceLineEndings(\"\\n\");\nvar lines=s.Split('\\n');\n")==0,"a normalised read");
        }),
        ("Raw newline 1871: Replace with a « …x\\n… » snippet is found (the text stays as it was on a CRLF runner)",() =>
        {
            Check(Count("var s=File.ReadAllText(\"a\");\nvar t=s.Replace(\"a;\\n b\",\"c\");\n")==1,"Replace through a variable");
            Check(Count("var t=File.ReadAllText(\"a\").Trim().Replace(\"a;\\n b\",\"c\");\n")==1,"Replace in line after a chained call");
            Check(Count("var t=File.ReadAllText(\"a\").Replace(\"\\r\\n\",\"\\n\").Replace(\"a;\\n b\",\"c\");\n")==0,"a normalisation before it passes");
            Check(Count("var s=File.ReadAllText(\"a\");\nvar t=s.Replace(\"\\n\",\" \");\n")==0,"a « \\n » alone passes (each « \\r\\n » holds it)");
        }),
        ("Raw newline 1871: a read written in a comment or a string is not a read",() =>
        {
            Check(Count("// File.ReadAllText(\"a\").Contains(\"a;\\n b\")\n")==0,"line comment");
            Check(Count("/* var s=File.ReadAllText(\"a\");\nCheck(s.Contains(\"a;\\n b\"),\"x\"); */\n")==0,"block comment");
            Check(Count("var help=\"File.ReadAllText(x).Contains(y)\";\n")==0,"string literal");
            Check(Count("/* note */ var s=File.ReadAllText(\"a\");\nCheck(s.Contains(\"a;\\n b\"),\"x\");\n")==1,"a read after a comment on the same line is still found");
        }),
        ("Raw newline 1871: the class of a raw helper is the type that holds it, not a « class » written in a comment or a string; records and structs count",() =>
        {
            const string helper="    internal static string Read(string f)=>File.ReadAllText(f);\n";
            Check(Owner("internal static class Texts\n{\n    // read that class of files raw\n"+helper+"}\n")=="Texts","a « class » in a comment");
            Check(Owner("internal static class Texts\n{\n    private const string Note=\"class Other\";\n"+helper+"}\n")=="Texts","a « class » in a string");
            Check(Owner("internal static class Texts\n{\n    private const string Note=@\"a \"\"class\"\" Other\";\n"+helper+"}\n")=="Texts","a « class » in a verbatim string");
            Check(Owner("internal static class Texts\n{\n    private const string Note=\"\"\"\n        class Other\n        \"\"\";\n"+helper+"}\n")=="Texts","a « class » in a raw string");
            Check(Owner("internal record Texts(string A)\n{\n"+helper+"}\n")=="Texts","record");
            Check(Owner("internal readonly record struct Texts(string A)\n{\n"+helper+"}\n")=="Texts","record struct");
            Check(Owner("internal struct Texts\n{\n"+helper+"}\n")=="Texts","struct");
            Check(Owner("internal static class Outer\n{\n    internal static class Inner\n    {\n"+helper+"    }\n}\n")=="Outer.Inner","nested types joined by dots");
            Check(Owner("internal static class Before\n{\n}\ninternal static class Texts\n{\n"+helper+"}\n")=="Texts","a closed type before it does not hold it");
            var caller="void T(){Check(Texts.Read(\"a\").Contains(\"a;\\n b\"),\"x\");}\n";
            var shared=Pending1865Tests.RawHelpers("internal static class Texts\n{\n    // that class\n"+helper+"}\n").Single();
            Check(Pending1865Tests.RawNewlineSearches(caller,[(shared.Class+"."+shared.Name,shared.Property)]).Count==1,"its calls from another file are found");
        }),
        ("Raw newline 1871: a helper written in a comment is not a helper",() =>
        {
            Check(Pending1865Tests.RawHelpers("internal static class Texts\n{\n    // internal static string Read(string f)=>File.ReadAllText(f);\n}\n").Count==0,"commented out");
        }),
    ];
}
