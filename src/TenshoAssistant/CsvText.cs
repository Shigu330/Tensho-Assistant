namespace TenshoAssistant;

/// <summary>
/// X-790 (0.1812, idea I-054): the one CSV field escape of Tensho's exports (bank list, voyage history, notebooks, market board), instead of
/// four private copies: quoted, inner quotes doubled, and a leading = + - @ prefixed with an apostrophe so a spreadsheet never reads a field
/// as a formula. Numbers and dates are written by the exports themselves, not through it. Pure.
/// </summary>
public static class CsvText
{
    public static string Field(string value)
    {
        var safe=value.Length>0 && "=+-@".Contains(value[0]) ? "'"+value:value;
        return "\""+safe.Replace("\"","\"\"")+"\"";
    }
}
