using System.Reflection;

namespace TenshoAssistant.Desktop;

/// <summary>
/// X-151 (0.1157, GPT review of 11/11, accepted by the user): the contract between the desktop application and the plugin, written down.
/// The application builds the plugin's real main window from the built DLL and reaches some of its members by name (Host, Headless): a
/// member renamed or changed in the plugin compiles fine and, until now, turned a feature off in silence (most lookups are « ?. »). Every
/// such member is listed here, with its exact parameters; the headless run of Verify resolves the whole list against the plugin it packs
/// and fails when one is missing (so publish.sh and the release stop), and the interactive app writes the missing ones to its console.
/// DesktopContract1157Tests keeps the list complete: every name the application reflects on must be listed. X-151 (0.1238): the
/// application's working entries (pages, data, alarms, notices) are typed now (DesktopSurface, reached through MainWindow.DesktopSurfaceMap);
/// the names left are the window's construction (settings, texts, lexicon) and the headless run's own checks.
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
        new(Main,Kind.Method,"DesktopSurfaceMap",false,[],"the typed contract (DesktopSurface): pages, data, alarms, notices"),
        new(Main,Kind.Property,"PageEntries",false,null,"the navigation"),
        new(Main,Kind.Method,"UseSheetLoaders",false,["Dalamud.Plugin.Services.IDataManager","Dalamud.Plugin.Services.IPluginLog"],"the page loaders that read only the sheets (X-285)"),
        new(Main,Kind.Method,"ConfigureGarden",false,["Dalamud.Plugin.Services.IDataManager"],"the garden's seed list and crossbreeds (X-178)"),
        new(Main+"+PageEntry",Kind.Property,"Page",false,null,"the navigation"),
        new(Main+"+PageEntry",Kind.Property,"Label",false,null,"--step=entries (diagnostic)"),
        new(Main+"+PageEntry",Kind.Property,"Visible",false,null,"--step=entries (diagnostic)"),
        new(Main,Kind.Field,"jitWarmup",false,null,"the headless timing"),
        new("TenshoAssistant.Diagnostics.JitWarmup",Kind.Method,"Wait",false,["System.TimeSpan"],"the headless timing"),
        new("TenshoAssistant.Diagnostics.JitWarmup",Kind.Property,"Prepared",false,null,"the headless timing"),
        new("TenshoAssistant.Diagnostics.JitWarmup",Kind.Property,"Skipped",false,null,"the headless timing"),
        new("TenshoAssistant.Diagnostics.JitWarmup",Kind.Property,"Elapsed",false,null,"the headless timing"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Type,"",false,null,"the texts"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Method,"SelectInitialLanguage",false,null,"the texts"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Method,"Format",false,null,"the texts"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Method,"Has",false,null,"the texts"),
        new("TenshoAssistant.Localization.LocalizationManager",Kind.Property,"KeyRead",false,null,"the keys a page asks, for the reference pictures (X-285)"),
        new("TenshoAssistant.Caching.DataCache",Kind.Type,"",false,null,"the catalogue cache"),
        new("TenshoAssistant.Maps.MapCatalogService",Kind.Property,"State",false,null,"the headless check of the map catalogue"),
        new("TenshoAssistant.Maps.MapCatalogState",Kind.Property,"Catalog",false,null,"the headless check of the map catalogue"),
        new("TenshoAssistant.Maps.MapCatalogState",Kind.Property,"Error",false,null,"the headless check of the map catalogue"),
        new("TenshoAssistant.Gathering.WeatherCatalogService",Kind.Property,"State",false,null,"the headless check of the weather catalogue"),
        new("TenshoAssistant.Gathering.WeatherCatalogState",Kind.Property,"Catalog",false,null,"the headless check of the weather catalogue"),
        new("TenshoAssistant.Gathering.WeatherCatalogState",Kind.Property,"Error",false,null,"the headless check of the weather catalogue"),
        new(Main,Kind.Field,"save",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new(Main,Kind.Field,"configuration",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new(Main,Kind.Field,"alarmStatus",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new(Main,Kind.Method,"AddAlarms",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new(Main,Kind.Property,"MyToolsStore",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new("TenshoAssistant.MyTools.ToolTemplateStore",Kind.Property,"ReadOnly",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new("TenshoAssistant.Configuration",Kind.Property,"Alarms",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new("TenshoAssistant.Alarms.TenshoAlarm",Kind.Type,"",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new("TenshoAssistant.Alarms.TenshoAlarm",Kind.Property,"Label",false,null,"the headless check that nothing is saved in the app (X-180)"),
        new("TenshoAssistant.Configuration",Kind.Property,"ThemeMode",false,null,"--theme= (X-154)"),
        new("TenshoAssistant.Configuration",Kind.Property,"NavigationMode",false,null,"the headless folded navigation (X-154)"),
        new("TenshoAssistant.Configuration",Kind.Property,"GardenBeds",false,null,"the trial patch of the garden views (X-178)"),
        new("TenshoAssistant.Configuration",Kind.Property,"GardenDetailedView",false,null,"the trial patch of the garden views (X-178)"),
        new("TenshoAssistant.Reminders.GardenBed",Kind.Type,"",false,null,"the trial patch of the garden views (X-178)"),
        new("TenshoAssistant.Reminders.GardenBed",Kind.Property,"Place",false,null,"the trial patch of the garden views (X-178)"),
        new("TenshoAssistant.Reminders.GardenBed",Kind.Property,"Seed",false,null,"the trial patch of the garden views (X-178)"),
        new("TenshoAssistant.Reminders.GardenBed",Kind.Property,"SeedId",false,null,"the trial patch of the garden views (X-178)"),
        new("TenshoAssistant.Reminders.GardenBed",Kind.Property,"PlantedAt",false,null,"the trial patch of the garden views (X-178)"),
        new("TenshoAssistant.Reminders.GardenBed",Kind.Property,"GrowHours",false,null,"the trial patch of the garden views (X-178)"),
        new("TenshoAssistant.Reminders.GardenBed",Kind.Property,"TendHours",false,null,"the trial patch of the garden views (X-178)"),
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
