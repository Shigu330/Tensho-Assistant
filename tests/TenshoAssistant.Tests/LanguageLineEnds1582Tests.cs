// 0.1582 (X-642 M1, review of 0.1581): a lone carriage return after ItemLinkTip (« \r\r\n », X-638) made git see fr.json as
// binary: the whole file was stored in CRLF and the diff of one key rewrote its 8 536 lines. The language files carry no carriage
// return that is not the first half of a line end, so git keeps them as text (index in LF).
internal static class LanguageLineEnds1582Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Folder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName,"src","TenshoAssistant","Localization","Languages"))) dir = dir.Parent;
        if (dir == null) throw new Exception("language folder not found");
        return Path.Combine(dir.FullName,"src","TenshoAssistant","Localization","Languages");
    }

    internal static List<int> LoneCarriageReturns(byte[] bytes)
    {
        var found = new List<int>();
        for (var i = 0; i < bytes.Length; i++)
            if (bytes[i] == (byte)'\r' && (i + 1 >= bytes.Length || bytes[i + 1] != (byte)'\n')) found.Add(i);
        return found;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Language line ends 1582: no lone carriage return in any language file",() =>
        {
            var files = Directory.GetFiles(Folder(),"*.json");
            Check(files.Length >= 4,"the four language files are read");
            foreach (var file in files)
            {
                var lone = LoneCarriageReturns(File.ReadAllBytes(file));
                Check(lone.Count == 0,$"{Path.GetFileName(file)}: lone carriage return at byte {(lone.Count > 0 ? lone[0] : -1)}");
            }
        }),
        ("Language line ends 1582: the check finds « \r\r\n » and lets CRLF and LF pass",() =>
        {
            Check(LoneCarriageReturns("a\",\r\r\n  b"u8.ToArray()).Count == 1,"« \r\r\n » is found");
            Check(LoneCarriageReturns("a\r\nb\nc\r\n"u8.ToArray()).Count == 0,"CRLF and LF pass");
            Check(LoneCarriageReturns("a\r"u8.ToArray()).Count == 1,"a final lone carriage return is found");
        }),
    ];
}
