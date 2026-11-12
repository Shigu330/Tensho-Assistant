using System.Reflection;

namespace TenshoAssistant.Desktop;

/// <summary>
/// X-151 (0.1157, GPT review of 11/11, accepted by the user): the contract between the desktop application and the plugin, written down.
/// The application builds the plugin's real main window from the built DLL and reaches some of its members by name (Host, Headless): a
/// member renamed or changed in the plugin compiles fine and, until now, turned a feature off in silence (most lookups are « ?. »). Every
/// such member is listed here, with its exact parameters; the headless run of Verify resolves the whole list against the plugin it packs
/// and fails when one is missing (so publish.sh and the release stop), and the interactive app writes the missing ones to its console.
/// DesktopContract1157Tests keeps the list complete: every name the application reflects on must be listed. A typed interface in place of
/// these names is the next step (the plugin's DLL is loaded at run time from a rewritten copy, so a shared type is not the same type yet).
/// </summary>
internal static class DesktopContract
{
    internal enum Kind { Type, Method, Field, Property }

    /// <summary>One member the application reaches: its type's full name, kind, name (empty for a type), static or not, parameters
    /// (Type.ToString() of each; null: any), and what it serves (English, for the report).</summary>
    internal sealed record Member(string Type,Kind Kind,string Name,bool Static,string[]? Parameters,string Use);

    private const string Main="TenshoAssistant.MainWindow";

    public static readonly Member[] Members=
    [
        new("TenshoAssistant.Configuration",Kind.Type,"",false,null,"the saved settings"),
        new("TenshoAssistant.Configuration",Kind.Property,"LanguageCode",false,null,"the plugin's language"),
        new(Main,Kind.Type,"",false,null,"the plugin's window"),
        new(Main+"+Page",Kind.Type,"",false,null,"the pages"),
        new(Main,Kind.Field,"page",false,null,"the page shown"),
        new(Main,Kind.Property,"PageEntries",false,null,"the navigation"),
        new(Main,Kind.Field,"pageEntries",false,null,"the navigation without the in-game pages"),
        new(Main,Kind.Field,"pageGroups",false,null,"the navigation without the in-game pages"),
        new(Main,Kind.Property,"LexiconLoad",false,null,"the lexicon"),
        new(Main+"+PageEntry",Kind.Property,"Page",false,null,"the navigation"),
        new(Main+"+PageEntry",Kind.Property,"Label",false,null,"--step=entries (diagnostic)"),
        new(Main+"+PageEntry",Kind.Property,"Visible",false,null,"--step=entries (diagnostic)"),
        new(Main+"+PageEntry",Kind.Field,"<Visible>k__BackingField",false,null,"the in-game pages hidden"),
        new(Main+"+PageEntry",Kind.Method,"<Clone>$",false,[],"the in-game pages hidden"),
        new(Main,Kind.Method,"UseDesktopData",false,["System.String","System.String","System.Action`2[System.Exception,System.String]"],"the saved data"),
        new(Main,Kind.Method,"DesktopDueAlarms",false,["System.DateTimeOffset","System.Boolean"],"the alarms and the garden ring"),
        new(Main,Kind.Method,"DesktopAlarmNotification",false,[],"the Windows notification"),
        new(Main,Kind.Method,"DesktopLinkNotices",false,["System.String[]","System.Boolean"],"the game's other alerts ring"),
        new(Main,Kind.Method,"DesktopAlarmHeld",false,["System.UInt32[]","System.Int64[]"],"an item already gathered stays silent"),
        new(Main,Kind.Method,"OpenWeather",false,null,"--zone (diagnostic)"),
        new(Main,Kind.Field,"jitWarmup",false,null,"the headless timing"),
        new("TenshoAssistant.Diagnostics.JitWarmup",Kind.Method,"Wait",false,["System.TimeSpan"],"the headless timing"),
        new("TenshoAssistant.Diagnostics.JitWarmup",Kind.Property,"Prepared",false,null,"the headless timing"),
        new("TenshoAssistant.Diagnostics.JitWarmup",Kind.Property,"Skipped",false,null,"the headless timing"),
        new("TenshoAssistant.Diagnostics.JitWarmup",Kind.Property,"Elapsed",false,null,"the headless timing"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Type,"",false,null,"the texts"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Method,"SelectInitialLanguage",false,null,"the texts"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Method,"Format",false,null,"the texts"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Method,"Has",false,null,"the texts"),
        new("TenshoAssistant.Lexicon.LexiconLoader",Kind.Method,"Read",true,null,"the lexicon"),
        new("TenshoAssistant.TenshoDataDatabase",Kind.Method,"CopyForDesktop",true,["System.String","System.String"],"the copy of the player's data file"),
        new("TenshoAssistant.TenshoDataDatabase",Kind.Method,"CopyDiscoveriesForDesktop",true,["System.String","System.String"],"the copy of the discoveries"),
        new("TenshoAssistant.Caching.DataCache",Kind.Type,"",false,null,"the catalogue cache"),
        new("TenshoAssistant.Maps.MapCatalogService",Kind.Property,"State",false,null,"the headless check of the map catalogue"),
        new("TenshoAssistant.Maps.MapCatalogState",Kind.Property,"Catalog",false,null,"the headless check of the map catalogue"),
        new("TenshoAssistant.Maps.MapCatalogState",Kind.Property,"Error",false,null,"the headless check of the map catalogue"),
        new("TenshoAssistant.Gathering.WeatherCatalogService",Kind.Property,"State",false,null,"the headless check of the weather catalogue"),
        new("TenshoAssistant.Gathering.WeatherCatalogState",Kind.Property,"Catalog",false,null,"the headless check of the weather catalogue"),
        new("TenshoAssistant.Gathering.WeatherCatalogState",Kind.Property,"Error",false,null,"the headless check of the weather catalogue"),
    ];

    private const BindingFlags Any=BindingFlags.Public|BindingFlags.NonPublic;

    /// <summary>The members of the list the plugin does not have (as « Type.Name (use) »); empty when the contract holds.</summary>
    public static IReadOnlyList<string> Missing(Func<string,Type?> typeOf)
    {
        var missing=new List<string>();
        foreach(var member in Members)
        {
            var type=typeOf(member.Type);
            if(type is null || !Has(type,member))missing.Add($"{member.Type}{(member.Name.Length>0 ? "."+member.Name : "")} ({member.Use})");
        }
        return missing;
    }

    /// <summary>The plugin's own lookup: a type by its full name (« A+B » for a nested type).</summary>
    public static IReadOnlyList<string> Missing(Assembly plugin)=>Missing(name=>plugin.GetType(name,false));

    private static bool Has(Type type,Member member)
    {
        var flags=Any|(member.Static ? BindingFlags.Static : BindingFlags.Instance);
        return member.Kind switch
        {
            Kind.Type=>true,
            Kind.Field=>type.GetField(member.Name,flags) is not null,
            Kind.Property=>type.GetProperty(member.Name,flags) is not null,
            _=>type.GetMethods(flags).Any(method=>method.Name==member.Name
                && (member.Parameters is null || method.GetParameters().Select(p=>p.ParameterType.ToString()).SequenceEqual(member.Parameters))),
        };
    }
}
