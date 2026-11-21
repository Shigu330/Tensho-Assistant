namespace TenshoAssistant.UI;

/// <summary>
/// X-601 (0.1677, user request of 18/11, D-199): the pages grouped behind one side bar entry. A family is a side bar entry (its root page)
/// and the pages that name it as their parent; each is a tab of the one page header. Pure: the window gives its page list and says which
/// pages can be reached here (shown, and listed on this side: the game or the desktop application).
/// </summary>
public static class PageFamilies
{
    /// <summary>One page of the list: its root is itself (a side bar entry) or its parent.</summary>
    public readonly record struct Member<T>(T Page,T? Parent,bool InSideBar,bool IsTab) where T:struct;

    /// <summary>The side bar entry that stays selected on this page.</summary>
    public static T Root<T>(IReadOnlyList<Member<T>> pages,T page) where T:struct
    {
        foreach(var member in pages)
            if(Same(member.Page,page))return member.Parent ?? page;
        return page;
    }

    /// <summary>The pages of a family, in the order of the list (the order of its tabs).</summary>
    public static IEnumerable<T> Of<T>(IReadOnlyList<Member<T>> pages,T root) where T:struct
        => pages.Where(member=>Same(member.Page,root) || member.Parent is { } parent && Same(parent,root)).Select(member=>member.Page);

    /// <summary>
    /// The page a click on a side bar entry opens: the tab last shown in its family when it can still be reached, else the first page of the
    /// family that can; null when none can (the entry is then not drawn). A page alone is a family of one.
    /// </summary>
    public static T? Target<T>(IReadOnlyList<Member<T>> pages,T root,T? last,Func<T,bool> reachable) where T:struct
    {
        T? first=null;
        foreach(var page in Of(pages,root))
        {
            if(!reachable(page))continue;
            if(last is { } remembered && Same(remembered,page))return page;
            first??=page;
        }
        return first;
    }

    /// <summary>
    /// What is wrong in a page list, one line per fault (empty: every page can be reached): a page that is neither a side bar entry nor a
    /// tab of one, a parent that is not a side bar entry, a tab whose family has no tab on its root, or a family page without a tab.
    /// </summary>
    public static IReadOnlyList<string> Faults<T>(IReadOnlyList<Member<T>> pages) where T:struct
    {
        var faults=new List<string>();
        foreach(var member in pages)
        {
            if(member.Parent is not { } parent)
            {
                if(!member.InSideBar)faults.Add($"{member.Page}: neither a side bar entry nor a tab of one (orphan)");
                else if(!member.IsTab && pages.Any(other=>other.Parent is { } named && Same(named,member.Page)))faults.Add($"{member.Page}: a family whose own page is not a tab");
                continue;
            }
            if(member.InSideBar)faults.Add($"{member.Page}: a side bar entry cannot have a parent");
            if(!member.IsTab)faults.Add($"{member.Page}: a page of a family without a tab");
            var roots=pages.Where(other=>Same(other.Page,parent)).ToArray();
            if(roots.Length!=1 || !roots[0].InSideBar || roots[0].Parent is not null)faults.Add($"{member.Page}: its parent {parent} is not a side bar entry");
        }
        return faults;
    }

    /// <summary>
    /// The folded categories kept when the side bar's categories change: the saved names that are still categories; null (never chosen)
    /// stays null, so the defaults still apply. Nothing else is lost: a category that no longer exists has nothing left to fold.
    /// </summary>
    public static HashSet<string>? KeepFolded(HashSet<string>? saved,IEnumerable<string> categories)
    {
        if(saved is null)return null;
        var known=categories.ToHashSet(StringComparer.Ordinal);
        return saved.Count(known.Contains)==saved.Count ? saved:new HashSet<string>(saved.Where(known.Contains),StringComparer.Ordinal);
    }

    /// <summary>X-601 (0.1681, D-199): the categories the side bar still draws as a folding header; every other entry is listed flat.</summary>
    public static readonly IReadOnlySet<string> SideBarCategories=new HashSet<string>(StringComparer.Ordinal){"Tracking"};

    /// <summary>The side bar category of a page's group: itself when the side bar still has it, else null (listed flat).</summary>
    public static string? SideBarCategory(string? category)=>category is not null && SideBarCategories.Contains(category) ? category:null;

    /// <summary>The entries that follow each other under the same key, as groups in the order of the list (a key can come back later).</summary>
    public static IReadOnlyList<(string? Key,T[] Items)> Runs<T>(IReadOnlyList<T> entries,Func<T,string?> key)
    {
        var runs=new List<(string? Key,T[] Items)>();
        var current=new List<T>();
        string? currentKey=null;
        foreach(var entry in entries)
        {
            var entryKey=key(entry);
            if(current.Count>0 && !string.Equals(entryKey,currentKey,StringComparison.Ordinal)){runs.Add((currentKey,current.ToArray()));current.Clear();}
            currentKey=entryKey;
            current.Add(entry);
        }
        if(current.Count>0)runs.Add((currentKey,current.ToArray()));
        return runs;
    }

    private static bool Same<T>(T left,T right) where T:struct=>EqualityComparer<T>.Default.Equals(left,right);
}
