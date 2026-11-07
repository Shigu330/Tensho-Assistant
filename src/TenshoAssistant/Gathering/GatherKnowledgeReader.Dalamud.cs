using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gathering;

/// <summary>
/// 0.518 (user report: a legendary point « open now » that is not there): what each point asks (GatheringSubCategory, docs/evidence/0.518-gather-tomes.md) and, every ten seconds on the
/// framework thread, the tomes read and the levels of the two gathering jobs. The tome's bit is its sheet's Quest column, as FFXIVClientStructs documents for these arrays. Unknown stays unknown: nothing read, nothing said.
/// </summary>
internal sealed unsafe class GatherKnowledgeReader : IDisposable
{
    private readonly IFramework framework;private readonly IClientState client;private readonly IPlayerState player;private readonly IPluginLog log;
    private readonly Dictionary<uint,GatherPointNeed> needs=[];
    private readonly Dictionary<uint,GatherPointKind> kinds=[];
    private readonly ClassJob? miner,botanist;
    private volatile GatherKnowledge? known;
    private DateTimeOffset nextRead;private string? logged;private bool failed;

    public GatherKnowledgeReader(IDataManager data,IFramework framework,IClientState client,IPlayerState player,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.player=player;this.log=log;
        try
        {
            var jobs=data.GetExcelSheet<ClassJob>();
            miner=jobs.GetRowOrDefault(GatherPointAccess.Miner);botanist=jobs.GetRowOrDefault(GatherPointAccess.Botanist);
            foreach(var point in data.GetExcelSheet<GatheringPoint>())
            {
                var at=point.GatheringPointBase.RowId;
                if(at==0 || !point.GatheringPointBase.IsValid)continue;
                var job=GatherPointAccess.JobOf(point.GatheringPointBase.Value.GatheringType.RowId);
                var sub=point.GatheringSubCategory;
                var tome=sub.RowId!=0 && sub.IsValid && sub.Value.Item.RowId!=0 ? sub.Value.Item.ValueNullable?.Name.ExtractText():null;
                var need=string.IsNullOrEmpty(tome) ? new GatherPointNeed(job,null,null):new GatherPointNeed(job,(int)sub.Value.Quest.RowId,tome);
                if(!needs.TryGetValue(at,out var seen) || seen.TomeBit is null)needs[at]=need;
                if(GatherPointKinds.Of(point.Type) is var kind and not GatherPointKind.Normal)kinds.TryAdd(at,kind);
            }
        }
        catch(Exception error){needs.Clear();kinds.Clear();log.Warning(error,"Gathering point needs unavailable (perhaps after a game update).");}
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);
    }

    public GatherPointKind Kind(uint basePoint)=>kinds.GetValueOrDefault(basePoint);
    public GatherPointAccess? Access(uint basePoint)=>GatherPointAccess.Of(needs.GetValueOrDefault(basePoint),known);

    private void Update(IFramework unused)
    {
        var now=DateTimeOffset.UtcNow;
        if(failed || now<nextRead)return;
        nextRead=now.AddSeconds(10);
        if(!client.IsLoggedIn || !player.IsLoaded){known=null;return;}
        try
        {
            var state=PlayerState.Instance();
            if(state==null){known=null;return;}
            var minerTomes=GatherTomeBits.Of(state->UnlockedMinerFolkloreTomes);var botanistTomes=GatherTomeBits.Of(state->UnlockedBotanistFolkloreTomes);
            var levels=(miner is { } one ? player.GetClassJobLevel(one):(short)0,botanist is { } two ? player.GetClassJobLevel(two):(short)0);
            known=new(levels.Item1,levels.Item2,minerTomes,botanistTomes);
            // Said once per change, so a wrong reading can be told from the game's own tomes.
            var said=$"miner {levels.Item1} [{string.Join(",",minerTomes)}], botanist {levels.Item2} [{string.Join(",",botanistTomes)}]";
            if(said!=logged){logged=said;log.Information("Gathering knowledge: {State}",said);}
        }
        catch(Exception error){failed=true;known=null;log.Warning(error,"Gathering tomes unavailable (perhaps after a game update).");}
    }

    public void Dispose()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);
}
