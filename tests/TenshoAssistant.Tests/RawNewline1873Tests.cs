// 0.1873 (X-868, review 0.1871 M2): the ratchet of the raw newline searches (Pending1865Tests) finds a split's « \n » at any place
// among the separators written in the call: several separators, an array (« new[]{…} », « new char[]{…} », « new string[]{…} ») or a
// collection expression (« […] »). A split whose separators also take the « \r » still passes. Not covered on purpose (reason in
// Pending1865Tests, NewlineSearch): separators held in a variable or a field.
internal static class RawNewline1873Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static int Count(string source)=>Pending1865Tests.RawNewlineSearches(source).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raw newline 1873: a split on an array or a collection of separators holding « \\n » is found",() =>
        {
            Check(Count("var lines=File.ReadAllText(\"a\").Split(new[]{'\\n'});\n")==1,"new[]{'\\n'}");
            Check(Count("var lines=File.ReadAllText(\"a\").Split(new char[] { '\\n' });\n")==1,"new char[] { '\\n' }");
            Check(Count("var lines=File.ReadAllText(\"a\").Split(new string[]{\"\\n\"},StringSplitOptions.None);\n")==1,"new string[]{\"\\n\"}");
            Check(Count("var lines=File.ReadAllText(\"a\").Split(['\\n']);\n")==1,"collection expression ['\\n']");
            Check(Count("var s=File.ReadAllText(\"a\");\nvar lines=s.Split([\"\\n\"],StringSplitOptions.None);\n")==1,"collection expression, through a variable");
            Check(Count("var lines=File.ReadAllText(\"a\").Split(new[]{';','\\n'});\n")==1,"« \\n » after another separator");
            Check(Count("var lines=File.ReadAllText(\"a\").Split(';','\\n');\n")==1,"several separators, no array");
        }),
        ("Raw newline 1873: a split whose separators also take the « \\r » passes; one without « \\n » is not a newline split",() =>
        {
            Check(Count("var lines=File.ReadAllText(\"a\").Split(new[]{'\\r','\\n'});\n")==0,"new[]{'\\r','\\n'}");
            Check(Count("var lines=File.ReadAllText(\"a\").Split([\"\\r\\n\",\"\\n\"],StringSplitOptions.None);\n")==0,"[\"\\r\\n\",\"\\n\"]");
            Check(Count("var lines=File.ReadAllText(\"a\").Split(new[]{';',','});\n")==0,"no « \\n » among the separators");
        }),
    ];
}
