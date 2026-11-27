// 0.1872 (X-867, review 0.1871 M1 and M3): the ratchet of the raw newline searches (Pending1865Tests). M1: a split on « \n » passes
// only when the « \r » or the Trim comes from the split itself to the end of its statement (first « ; » of the code, on its line or
// the next); a « \r » or a Trim before the split, or in the next statement, no longer lets it pass. Not covered on purpose (reason in
// Pending1865Tests, Handled): a Trim or a « \r » after the split in the same statement but on another expression. M3: CodeOnly reads
// « @$"…" » as verbatim, like « $@"…" »: a « \" » ends it, so a read written after it on the same line is still found.
internal static class RawNewline1872Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static int Count(string source)=>Pending1865Tests.RawNewlineSearches(source).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raw newline 1872: a « \\r » or a Trim lets a split pass only from the split to the end of its statement",() =>
        {
            Check(Count("var s=File.ReadAllText(\"a\");\nvar t=x.Trim();var lines=s.Split('\\n');\n")==1,"a Trim before the split, same line");
            Check(Count("var s=File.ReadAllText(\"a\");\nvar lines=s.Split('\\n');\nvar name=other.Trim();\n")==1,"a Trim in the next statement");
            Check(Count("var r=\"\\r\";var lines=File.ReadAllText(\"a\").Split('\\n');\n")==1,"a « \\r » before the split, in line");
            Check(Count("var lines=File.ReadAllText(\"a\").Split('\\n');var r=x.TrimEnd('\\r');\n")==1,"a « \\r » in the next statement, in line");
            Check(Count("var lines=File.ReadAllText(\"a\").Split('\\n')\n    .Select(line=>line.TrimEnd('\\r'));\n")==0,"trimmed on the next line, same statement");
            Check(Count("var s=File.ReadAllText(\"a\");\nvar lines=s.Split('\\n',StringSplitOptions.TrimEntries);var t=x;\n")==0,"TrimEntries in the split");
            Check(Count("var s=File.ReadAllText(\"a\");\nvar lines=s.Split(\";\\n\").Select(l=>l.Trim());\n")==0,"a « ; » in a literal does not end the statement");
        }),
        ("Raw newline 1872: « @$\"…\" » is verbatim, like « $@\"…\" »",() =>
        {
            Check(Count("var p=@$\"{x}\\\";Check(File.ReadAllText(\"a\").Contains(\"a;\\n b\"),\"x\");\n")==1,"@$ verbatim ending with a backslash");
            Check(Count("var p=$@\"{x}\\\";Check(File.ReadAllText(\"a\").Contains(\"a;\\n b\"),\"x\");\n")==1,"$@ verbatim ending with a backslash");
            Check(Count("var p=@$\"File.ReadAllText(x).Contains(\"\"a;\\n b\"\")\";\n")==0,"a read inside an @$ literal is not a read");
            Check(Pending1865Tests.CodeOnly("x=@$\"a\\\";y=1;")=="x=@$\"  \";y=1;","CodeOnly blanks the @$ literal only");
        }),
    ];
}
