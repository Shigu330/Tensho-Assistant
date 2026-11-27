using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// X-153 (0.1924, GPT review of 11/11, accepted by the user): the crafting family out of Plugin.cs, after the fashion family (0.1903). The
// crafting actions' loader, the live crafting reader, the recipe log, the rotation recorder and the craft log reader are each created in one
// install path and released in one release path of the plugin's registrations, in this file, with the window's crafting lists, food and
// medicine, meals, detection and Manipulation learned; Plugin.cs calls the two blocks where they stood, so the order of the registrations is
// the one of before. CraftFamily1924Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The crafting readers: actions' catalog, live crafting, food and medicine, meals, food and specialist detection, recipes crafted.</summary>
    private void RegisterCraftReaders(IDataManager data,IFramework framework,IPlayerState player,IClientState client,IGameGui gameGui,IObjectTable objects)
    {
        CraftActionCatalogLoader? craftActionLoader=null;
        registrations.Register(()=>craftActionLoader=new CraftActionCatalogLoader(craftActions,data,framework,log),()=>craftActionLoader?.Dispose());
        CraftLiveReader? craftLiveReader=null;
        registrations.Register(()=>craftLiveReader=new CraftLiveReader(craftLive,recipeCatalog,craftActions,itemCatalog,framework,player,client,gameGui,objects,log),()=>craftLiveReader?.Dispose());
        // A-02 (0.420): crafting food and medicine from the game's sheets (Item › ItemAction › ItemFood), built on first use.
        var craftConsumables=new Lazy<IReadOnlyList<Craft.CraftConsumable>>(()=>
        {
            try
            {
                var foods=data.GetExcelSheet<Lumina.Excel.Sheets.ItemFood>();var list=new List<Craft.CraftConsumable>();
                foreach(var item in data.GetExcelSheet<Lumina.Excel.Sheets.Item>().Where(row=>row.ItemAction.RowId!=0 && !row.Name.IsEmpty))
                {
                    if(item.ItemAction.ValueNullable is not { } action || action.Data.Count<2 || (uint)action.Data[0] is not (Craft.CraftConsumables.WellFed or Craft.CraftConsumables.Medicated)
                       || foods.GetRowOrDefault(action.Data[1]) is not { } food)continue;
                    var bonuses=food.Params.Where(param=>param.BaseParam.RowId is Craft.CraftConsumables.Craftsmanship or Craft.CraftConsumables.Control or Craft.CraftConsumables.Cp)
                        .Select(param=>new Craft.CraftBonus(param.BaseParam.RowId,param.IsRelative,param.Value,param.Max,param.ValueHQ,param.MaxHQ)).ToArray();
                    if(bonuses.Length>0)list.Add(new(item.RowId,item.Name.ExtractText(),(uint)action.Data[0]==Craft.CraftConsumables.Medicated,bonuses,action.Data[1]));
                }
                return list.OrderByDescending(item=>item.Bonuses.Sum(bonus=>bonus.MaxHq)).ThenBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
            }
            catch(Exception error){log.Warning(error,"Crafting food and medicine unavailable.");return [];}
        });
        mainWindow.CraftConsumableList=()=>craftConsumables.Value;
        // X-229 (0.1784): every meal (Well Fed, Status 48, with an ItemFood row) and all its bonuses, for the gear planner, built on first use.
        var gearMeals=new Lazy<IReadOnlyList<Craft.CraftConsumable>>(()=>
        {
            try
            {
                var foods=data.GetExcelSheet<Lumina.Excel.Sheets.ItemFood>();var list=new List<Craft.CraftConsumable>();
                foreach(var item in data.GetExcelSheet<Lumina.Excel.Sheets.Item>().Where(row=>row.ItemAction.RowId!=0 && !row.Name.IsEmpty))
                {
                    if(item.ItemAction.ValueNullable is not { } action || action.Data.Count<2 || (uint)action.Data[0]!=Craft.CraftConsumables.WellFed
                       || foods.GetRowOrDefault(action.Data[1]) is not { } food)continue;
                    var bonuses=food.Params.Where(param=>param.BaseParam.RowId!=0)
                        .Select(param=>new Craft.CraftBonus(param.BaseParam.RowId,param.IsRelative,param.Value,param.Max,param.ValueHQ,param.MaxHQ)).ToArray();
                    if(bonuses.Length>0)list.Add(new(item.RowId,item.Name.ExtractText(),false,bonuses,action.Data[1]));
                }
                return list.OrderBy(item=>item.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
            }
            catch(Exception error){log.Warning(error,"Meals for the gear planner unavailable.");return [];}
        });
        mainWindow.GearMealList=()=>gearMeals.Value;
        // 0.464: food, medicine and specialist read from the game for the simulator; a failure leaves them unknown.
        var craftDetectionWarned=false;
        mainWindow.CraftDetectedNow=job=>
        {
            try{return Craft.CraftDetectionReader.Read(objects,job);}
            catch(Exception error){if(!craftDetectionWarned){craftDetectionWarned=true;log.Warning(error,"Crafting food, medicine and specialist could not be read (perhaps after a game update).");}return null;}
        };
        // A-11 (0.421): recipes crafted at least once (QuestManager.IsRecipeComplete), read while a crafting view asks.
        Craft.RecipeLogReader? recipeLog=null;
        registrations.Register(()=>mainWindow.RecipeLog=recipeLog=new Craft.RecipeLogReader(framework,client,log,()=>recipeCatalog.State.Catalog),()=>recipeLog?.Dispose());
    }

    /// <summary>The crafting lists' file, the rotation recorder, Manipulation learned by the job and the craft log reader.</summary>
    private void RegisterCraftLog(IDalamudPluginInterface pluginInterface,Configuration configuration,IDataManager data,IFramework framework,IPlayerState player,
        IClientState client,IGameGui gameGui,IUnlockState unlocks,IGameInteropProvider interop)
    {
        // A-05 (0.433): crafting lists in a file of their own (like the Bank).
        mainWindow.CraftListStore=new Craft.CraftListStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"CraftLists.json"),(error,message)=>log.Warning(error,message));
        // A-14 (0.436): crafting rotation recorder (read-only hook on UseAction, only while recording).
        Craft.CraftRecorder? craftRecorder=null;
        registrations.Register(()=>mainWindow.CraftRecorder=craftRecorder=new Craft.CraftRecorder(interop,log,()=>craftActions.State.Catalog),()=>craftRecorder?.Dispose());
        // X-11 (0.419): Manipulation learned by the job, from its action row (CraftActionCatalog) and the game's unlock state.
        // 0.468: said once by job in the log, so a wrong detection can be told from the action's data.
        var manipulationLogged=new HashSet<uint>();
        mainWindow.ManipulationLearned=job=>
        {
            if(craftActions.State.Catalog?.Find(job,Craft.CraftActionKind.Manipulation) is not { } manipulation
                || data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(manipulation.NativeId) is not { } action)return false;
            var unlocked=unlocks.IsActionUnlocked(action);
            if(manipulationLogged.Add(job))log.Information("Manipulation for job {Job}: action {Action}, unlock link {Link}, unlocked {Unlocked}",job,manipulation.NativeId,action.UnlockLink.RowId,unlocked);
            return unlocked;
        };
        CraftLogReader? craftLogReader=null;
        registrations.Register(()=>craftLogReader=new CraftLogReader(mainWindow.CraftLog,recipeCatalog,configuration,framework,player,client,gameGui,log),()=>craftLogReader?.Dispose());
    }
}
