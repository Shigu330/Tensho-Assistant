namespace TenshoAssistant.Desktop;

/// <summary>How a page of the plugin's main window behaves in the desktop application (X-119).</summary>
public enum DesktopPageKind { AsIs, NeedsLink, InGame }

/// <summary>
/// The desktop application's page policy, one list (X-119, R33). Measured in X-118 (docs/evidence/x118-desktop-probe.md, inventory table):
/// InGame = the pages the user keeps in the game (hidden from the navigation; a link to one shows Home with a line saying so); NeedsLink =
/// the 17 pages whose own code calls a game-link class (their live parts stay empty and their actions are off until the link of step 2,
/// said by the status bar); AsIs = the 31 others, drawn from the plugin's services alone. A page added to the plugin and missing here makes
/// the headless run (Verify.ps1) and a test fail until it is classified: never forgotten.
/// </summary>
public static class DesktopPages
{
    public static readonly IReadOnlyList<string> InGame = ["Maps","CraftLive","Fashion"];

    public static readonly IReadOnlyList<string> NeedsLink =
    [
        "Home","Items","Storage","CraftSimulation","Gathering","GatheringCollectables","OceanFishing","IslandSanctuary","Weather","Reminders",
        "FreeCompany","Events","Collections","BlueMagic","Help","Settings","Diagnostics",
    ];

    public static readonly IReadOnlyList<string> AsIs =
    [
        "SortPreview","Market","CraftPresets","GatheringGp","MacroChain","Fates","NpcVendors","DutyLoot","MonsterLoot","Alarms","Garden","Quests",
        "AlliedSocieties","GrandCompanies","SpecialDeliveries","CustomDeliveries","Gear","Chocobo","Leves","HuntingLog","MaskedCarnivale",
        "RelicWeapons","FieldContent","DeepDungeons","Beastmaster","TripleTriad","MiniCactpot","Lexicon","Credits","MyTools","Discoveries",
    ];

    /// <summary>The page's kind, or null for a page that is not classified yet.</summary>
    public static DesktopPageKind? Kind(string page)
        => InGame.Contains(page) ? DesktopPageKind.InGame
            : NeedsLink.Contains(page) ? DesktopPageKind.NeedsLink
            : AsIs.Contains(page) ? DesktopPageKind.AsIs
            : null;

    /// <summary>The plugin's pages that are not classified here (empty when every page is).</summary>
    public static IReadOnlyList<string> Unclassified(IEnumerable<string> pages)=>pages.Where(page=>Kind(page) is null).ToArray();

    /// <summary>The message of a run that meets unclassified pages.</summary>
    public static string UnclassifiedMessage(IReadOnlyList<string> pages)
        => $"Unclassified page(s) of the plugin: {string.Join(", ",pages)}. Add each to one list of src/TenshoAssistant.Desktop/DesktopPages.cs "
           +"(AsIs: drawn from the plugin's services; NeedsLink: its code calls a game-link class; InGame: stays in the game), then run Verify again.";

    /// <summary>The page to show instead of the one asked: Home for a page that stays in the game, else the page itself.</summary>
    public static string Shown(string asked)=>Kind(asked)==DesktopPageKind.InGame ? "Home" : asked;

    /// <summary>The language key naming the live parts of a NeedsLink page (Desktop.Live&lt;Page&gt;), or null for the other pages.</summary>
    public static string? LiveKey(string page)=>Kind(page)==DesktopPageKind.NeedsLink ? "Desktop.Live"+page : null;
}
