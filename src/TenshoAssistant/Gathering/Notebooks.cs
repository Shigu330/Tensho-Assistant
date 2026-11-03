using System.Globalization;
using System.Text;

namespace TenshoAssistant.Gathering;

/// <summary>Which notebook an item belongs to.</summary>
public enum NotebookKind { Fishing, Spearfishing, Gathering }

/// <summary>One notebook entry: its kind, the item, and whether the character has it (caught / gathered).</summary>
public sealed record NotebookEntry(NotebookKind Kind,uint ItemId,bool Done);

/// <summary>The character's notebooks as copied: entries by item (an item in two notebooks counts as done when done in either).</summary>
public sealed record NotebookState(IReadOnlyList<NotebookEntry> Entries,DateTimeOffset ReadAt)
{
    private readonly Dictionary<uint,bool> byItem=Entries.GroupBy(entry=>entry.ItemId).ToDictionary(group=>group.Key,group=>group.Any(entry=>entry.Done));
    /// <summary>True / false for an item of a notebook, null for an item in none.</summary>
    public bool? Done(uint itemId)=>byItem.TryGetValue(itemId,out var done) ? done:null;
    public (int Done,int Total) Count(NotebookKind kind)=>(Entries.Count(entry=>entry.Kind==kind && entry.Done),Entries.Count(entry=>entry.Kind==kind));
}

/// <summary>g, R-05 (0.402): the notebooks' CSV export (user's choice: clipboard and CSV file).</summary>
public static class Notebooks
{
    /// <summary>One line per entry: kind, item id, item name, done (yes / no). Fields quoted; a leading = + - @ is neutralised.</summary>
    public static string Csv(IEnumerable<NotebookEntry> entries,Func<uint,string> itemName)
    {
        var text=new StringBuilder("Notebook,ItemId,Item,Done\n");
        foreach(var entry in entries.OrderBy(entry=>entry.Kind).ThenBy(entry=>entry.ItemId))
            text.Append(string.Join(",",Quote(entry.Kind.ToString()),entry.ItemId.ToString(CultureInfo.InvariantCulture),Quote(itemName(entry.ItemId)),entry.Done ? "yes":"no")).Append('\n');
        return text.ToString();
    }
    private static string Quote(string value)
    {
        var safe=value.Length>0 && "=+-@".Contains(value[0]) ? "'"+value:value;
        return "\""+safe.Replace("\"","\"\"")+"\"";
    }
}
