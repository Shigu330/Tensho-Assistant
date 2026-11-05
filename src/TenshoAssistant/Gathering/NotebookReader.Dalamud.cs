using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gathering;

/// <summary>
/// g, R-05 (0.402): while the Gathering page asks (<see cref="Want"/>, 5 s window), copies every 5 seconds on the framework thread the
/// character's fishing log (PlayerState.IsFishCaught, by FishParameter row — the same index GatherBuddy, Apache-2.0, reads), spearfishing
/// log (IsSpearfishCaught, by SpearfishingItem row) and gathering log (QuestManager.IsGatheringItemGathered, a game function called only
/// once resolved, by GatheringItem row; to confirm in game, docs/evidence/0.402-notebooks.md). Cleared at logout; failures logged once.
/// </summary>
public sealed unsafe class NotebookReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private (uint Row,uint Item)[]? fish,spear,gathering;
    private NotebookState? state;
    // 0.404 (user report « notebooks still loading »): rows the game refuses (index outside its table: spearfishing rows 30000 and up give
    // 10000 and more for a table of 305, docs/evidence/0.402-notebooks.md) are set aside once and stay unknown; one refused row no longer
    // fails the whole read.
    private readonly HashSet<(NotebookKind,uint)> refused=[];
    private long wantedUntil;
    private DateTimeOffset nextRead;
    private bool disposed,warned;
    public NotebookState? State=>Volatile.Read(ref state);
    private bool unavailable;
    /// <summary>R-14 (0.454): the last read failed: the page says it instead of « Reading… » for ever.</summary>
    public bool Unavailable=>Volatile.Read(ref unavailable);

    public NotebookReader(IFramework framework,IClientState client,IDataManager data,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.data=data;this.log=log;
        framework.Update+=OnUpdate;
        try{client.Logout+=OnLogout;}catch{framework.Update-=OnUpdate;throw;}
    }

    public void Want()=>Volatile.Write(ref wantedUntil,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead || now.UtcTicks>Volatile.Read(ref wantedUntil) || !client.IsLoggedIn)return;
        nextRead=now.AddSeconds(5);
        try
        {
            fish??=data.GetExcelSheet<FishParameter>().Where(row=>row.IsInLog && row.Item.RowId!=0).Select(row=>(row.RowId,row.Item.RowId)).ToArray();
            spear??=data.GetExcelSheet<SpearfishingItem>().Where(row=>row.Item.RowId!=0).Select(row=>(row.RowId,row.Item.RowId)).ToArray();
            gathering??=data.GetExcelSheet<GatheringItem>().Where(row=>row.Item.RowId!=0 && row.RowId<=ushort.MaxValue && !row.IsHidden).Select(row=>(row.RowId,row.Item.RowId)).ToArray();
            var player=FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance();
            if(player==null || !player->IsLoaded)return;
            var entries=new List<NotebookEntry>(fish.Length+spear.Length+gathering.Length);
            Read(entries,NotebookKind.Fishing,fish,row=>player->IsFishCaught(row));
            Read(entries,NotebookKind.Spearfishing,spear,row=>player->IsSpearfishCaught(row));
            var gatheringReadable=(nint)QuestManager.MemberFunctionPointers.IsGatheringItemGathered!=0;
            if(gatheringReadable)
                Read(entries,NotebookKind.Gathering,gathering,row=>QuestManager.IsGatheringItemGathered((ushort)row));
            Volatile.Write(ref state,new NotebookState(entries,now,!gatheringReadable));
            warned=false;Volatile.Write(ref unavailable,false);
        }
        catch(Exception error)
        {
            nextRead=now.AddSeconds(30);Volatile.Write(ref unavailable,true);
            if(!warned){warned=true;log.Warning(error,"The gathering and fishing logs could not be read.");}
        }
    }

    // One notebook: each row asked on its own; a row the game refuses is set aside (unknown) and logged once with its notebook.
    private void Read(List<NotebookEntry> entries,NotebookKind kind,(uint Row,uint Item)[] rows,Func<uint,bool> done)
    {
        var skipped=0;
        foreach(var (row,item) in rows)
        {
            if(refused.Contains((kind,row))){skipped++;continue;}
            try{entries.Add(new(kind,item,done(row)));}
            catch(ArgumentOutOfRangeException){refused.Add((kind,row));skipped++;}
        }
        if(skipped>0 && loggedSkips.Add(kind))log.Information("{0} notebook: {1} rows outside the game's table, left unknown.",kind,skipped);
    }
    private readonly HashSet<NotebookKind> loggedSkips=[];

    private void OnLogout(int type,int code)=>Volatile.Write(ref state,null);

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
        client.Logout-=OnLogout;
    }
}
