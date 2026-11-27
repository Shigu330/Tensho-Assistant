using Dalamud.Plugin.Services;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

// X-153 (0.1926, GPT review of 11/11, accepted by the user): the gathering readers out of Plugin.cs, after the quests' family (0.1925); the
// gathering family of 0.1412 (Plugin.Gathering.cs: fish, bite times, notebooks, bait) is left as it was. The gathering window's reader, the
// spearfishing reader, the collectables' reader, the action bar locator and the gather knowledge reader are each created in one install path
// and released in one release path of the plugin's registrations, in this file, with what the window reads from them; Plugin.cs calls the two
// blocks where they stood, so the order of the registrations is the one of before. GatheringReadersFamily1926Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The gathering window, spearfishing, collectables, the action bar and the gathering actions' rows, read from the game.</summary>
    private void RegisterGatheringReaders(IDataManager data,IFramework framework,IPlayerState player,IClientState client,IGameGui gameGui,IObjectTable objects,
        ITargetManager targets)
    {
        GatheringObservationReader? gatheringReader=null;
        mainWindow.GatheringSlotRects=()=>gatheringReader?.SlotRects ?? [];
        mainWindow.GatheringAutoGather=()=>gatheringReader?.AutoGather;
        registrations.Register(()=>gatheringReader=new GatheringObservationReader(gatheringObservation,framework,player,client,gameGui,objects,log),()=>gatheringReader?.Dispose());
        CollectableReader? collectableReader=null;
        SpearfishingReader? spearfishingReader=null;
        registrations.Register(()=>mainWindow.Spearfishing=spearfishingReader=new SpearfishingReader(framework,gameGui,client,log,
            // 0.894 (user capture: node 32076 found, list of the zone shown): several points share one GatheringPointBase (32074–32076 → 465) and the catalogue
            // keeps one point per base, so the node is given as its base.
            ()=>(targets.Target is { ObjectKind:Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint } node ? node.BaseId
                // 0.892: no node targeted: the nearest targetable gathering point within 12 yalms.
                :objects.LocalPlayer is { } me ? objects.Where(item=>item.ObjectKind==Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint && item.IsTargetable
                    && System.Numerics.Vector3.Distance(item.Position,me.Position)<=12).OrderBy(item=>System.Numerics.Vector3.Distance(item.Position,me.Position))
                    .Select(item=>item.BaseId).FirstOrDefault():0u) is var found and not 0
                && data.GetExcelSheet<Lumina.Excel.Sheets.GatheringPoint>().GetRowOrDefault(found) is { } pointRow ? pointRow.GatheringPointBase.RowId:0u),()=>spearfishingReader?.Dispose());
        registrations.Register(()=>mainWindow.Collectables=collectableReader=new CollectableReader(framework,gameGui,client,data,log),()=>collectableReader?.Dispose());
        mainWindow.CollectableActionInfo=id=>data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id) is { } row ? (row.Name.ExtractText(),(uint)row.Icon,(int)row.PrimaryCostValue):null;
        // 0.480: status 757, Scrutiny (Status sheet, docs/evidence/0.479-collectable-window-values.md).
        mainWindow.CollectableActionStatus=id=>Gathering.CollectableActionState.Status(id);
        mainWindow.GatheringRecastLeft=id=>Gathering.CollectableActionState.RecastLeft(id);
        Gathering.ActionBarLocator? barLocator=null;
        registrations.Register(()=>mainWindow.BarLocator=barLocator=new Gathering.ActionBarLocator(framework,gameGui,log),()=>{mainWindow.BarLocator=null;barLocator?.Dispose();});
        // 0.517 (user report): IsActionUnlocked is false for a row without an unlock link, and 22 of the 24 gathering actions have none: they come with the level, which the plan checks (docs/evidence/0.517-gp-action-unlocks.md).
        var gatheringActionRows=new Dictionary<uint,(string,uint,string)?>();
        mainWindow.GatheringActionRow=id=>
        {
            if(gatheringActionRows.TryGetValue(id,out var known))return known;
            (string,uint,string)? found=null;
            try
            {
                if(data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id) is { } row)
                    found=(row.Name.ExtractText(),row.Icon,data.GetExcelSheet<Lumina.Excel.Sheets.ActionTransient>().GetRowOrDefault(id)?.Description.ExtractText() ?? "");
            }
            catch(Exception error){log.Warning(error,"Gathering action {Id} unavailable (perhaps after a game update).",id);}
            gatheringActionRows[id]=found;
            return found;
        };
    }

    /// <summary>What each gathering point asks (its tome) and, from the game, the tomes read and the gathering jobs' levels.</summary>
    private void RegisterGatherKnowledge(IDataManager data,IFramework framework,IClientState client,IPlayerState player)
    {
        Gathering.GatherKnowledgeReader? gatherKnowledge=null;
        registrations.Register(()=>{gatherKnowledge=new Gathering.GatherKnowledgeReader(data,framework,client,player,log);mainWindow.GatherAccess=gatherKnowledge.Access;mainWindow.GatherKind=gatherKnowledge.Kind;},()=>{mainWindow.GatherAccess=null;mainWindow.GatherKind=null;gatherKnowledge?.Dispose();});
    }
}
