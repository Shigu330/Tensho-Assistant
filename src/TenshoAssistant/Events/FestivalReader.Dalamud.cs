using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using TenshoAssistant.Npcs;
using Quest = Lumina.Excel.Sheets.Quest;

namespace TenshoAssistant.Events;

/// <summary>
/// R-09 (0.342): copies the running festivals (GameMain.ActiveFestivals: id and phase) on the framework thread, every 10 seconds while
/// logged in and only while the Events page asked for them in the last 5 seconds; guarded (R-14): a failure says « unavailable » until a
/// read succeeds. Builds once, on a worker, the quests of each festival (Quest.Festival) and the embedded English names. Removed on dispose.
/// </summary>
public sealed unsafe class FestivalReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly IDataManager data;
    private readonly CancellationTokenSource cancellation=new();
    private FestivalLayouts? layouts;
    private int layoutsStarted;
    private FestivalState? state;
    private IReadOnlyDictionary<uint,IReadOnlyList<uint>>? quests;
    private IReadOnlyDictionary<uint,string> names=new Dictionary<uint,string>();
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    private bool disposed,warned;

    public FestivalState? State=>Volatile.Read(ref state);
    public bool Unavailable { get; private set; }
    /// <summary>Quest rows of each festival (null while built, or when it could not be built: QuestsFailed).</summary>
    public IReadOnlyDictionary<uint,IReadOnlyList<uint>>? Quests=>Volatile.Read(ref quests);
    public bool QuestsFailed { get; private set; }
    public IReadOnlyDictionary<uint,string> Names=>Volatile.Read(ref names);
    /// <summary>Festival NPCs and guide names from the layouts (null until read, once, after the page first asked).</summary>
    public FestivalLayouts? Layouts=>Volatile.Read(ref layouts);
    public bool LayoutsFailed { get; private set; }
    public bool FrenchClient=>data.Language==ClientLanguage.French;

    public FestivalReader(IFramework framework,IClientState client,IDataManager data,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.log=log;this.data=data;
        var token=cancellation.Token;
        _=Task.Run(()=>
        {
            try
            {
                using(var stream=typeof(FestivalReader).Assembly.GetManifestResourceStream("TenshoAssistant.Events.Data.festival-names.csv")
                    ?? throw new InvalidOperationException("Embedded festival names are missing."))
                using(var reader=new StreamReader(stream))Volatile.Write(ref names,Festivals.ReadNames(reader));
                var byFestival=new Dictionary<uint,List<uint>>();
                foreach(var row in data.GetExcelSheet<Quest>())
                {
                    token.ThrowIfCancellationRequested();
                    if(row.Festival.RowId==0 || row.Name.IsEmpty)continue;
                    if(!byFestival.TryGetValue(row.Festival.RowId,out var list))byFestival[row.Festival.RowId]=list=[];
                    list.Add(row.RowId);
                }
                Volatile.Write(ref quests,byFestival.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<uint>)pair.Value.ToArray()));
            }
            catch(OperationCanceledException){}
            catch(Exception error){QuestsFailed=true;log.Warning(error,"Festival quests could not be built.");}
        },token);
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    /// <summary>Called by the page while it is shown (UI thread): keeps the reads going for 5 seconds; the first call reads the layouts.</summary>
    public void Want()
    {
        Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);
        if(Interlocked.Exchange(ref layoutsStarted,1)!=0)return;
        var token=cancellation.Token;
        _=Task.Run(()=>
        {
            try{Volatile.Write(ref layouts,ReadLayouts(token));}
            catch(OperationCanceledException){}
            catch(Exception error){LayoutsFailed=true;log.Warning(error,"Festival layouts could not be read.");}
        },token);
    }

    // Every territory's planevent.lgb (about 1 100 files, about one second, once): the event NPCs of each festival layer. Files are
    // checked as the vendor directory checks them (NpcShopLinks.ValidLayout) before Lumina parses them.
    private FestivalLayouts ReadLayouts(CancellationToken token)
    {
        var npcs=new Dictionary<uint,HashSet<(uint,uint)>>();
        foreach(var territory in data.GetExcelSheet<TerritoryType>())
        {
            token.ThrowIfCancellationRequested();
            var bg=territory.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
            if(cut<0)continue;
            var path=$"bg/{bg[..(cut+1)]}level/planevent.lgb";
            Lumina.Data.Files.LgbFile? file;
            try
            {
                if(!data.FileExists(path) || data.GetFile(path)?.Data is not { } bytes || !NpcShopLinks.ValidLayout(bytes))continue;
                file=data.GetFile<Lumina.Data.Files.LgbFile>(path);
            }
            catch(Exception error){log.Debug(error,"Festival layout unreadable: {0}",path);continue;}
            if(file is null)continue;
            foreach(var layer in file.Layers)
            {
                if(layer.FestivalID==0)continue;
                foreach(var instance in layer.InstanceObjects)
                    if(instance.AssetType==Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC && instance.Object is Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject npc)
                    {
                        if(!npcs.TryGetValue(layer.FestivalID,out var set))npcs[layer.FestivalID]=set=[];
                        set.Add((npc.ParentData.ParentData.BaseId,territory.RowId));
                    }
            }
        }
        // The guide: an NPC of the festival whose CustomTalk is named « Fes…Guide… » (probe: FesHlx2026Guide, FesXmx2026Guide…).
        var bases=data.GetExcelSheet<ENpcBase>();var talks=data.GetExcelSheet<CustomTalk>();var residents=data.GetExcelSheet<ENpcResident>();
        var guides=new Dictionary<uint,string>();
        foreach(var (festival,set) in npcs)
            foreach(var (npc,_) in set)
            {
                if(!bases.TryGetRow(npc,out var row))continue;
                var guide=row.ENpcData.Any(value=>value.RowId is >= 720896 and < 786432 && talks.TryGetRow(value.RowId,out var talk)
                    && talk.Name.ExtractText() is var name && name.StartsWith("Fes",StringComparison.Ordinal) && name.Contains("Guide",StringComparison.Ordinal));
                if(guide && residents.TryGetRow(npc,out var resident) && resident.Singular.ExtractText() is { Length:>0 } singular){guides[festival]=singular;break;}
            }
        return new(npcs.ToDictionary(pair=>pair.Key,pair=>(IReadOnlyList<(uint,uint)>)pair.Value.ToArray()),guides);
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead || now.UtcTicks>Volatile.Read(ref wantedUntilTicks))return;
        nextRead=now.AddSeconds(10);
        if(!client.IsLoggedIn){Volatile.Write(ref state,null);return;}
        try
        {
            var game=GameMain.Instance();
            if(game==null){Unavailable=true;return;}
            var slots=new List<(ushort,ushort)>();
            foreach(var festival in game->ActiveFestivals)slots.Add((festival.Id,festival.Phase));
            Volatile.Write(ref state,new FestivalState(now,Festivals.Running(slots)));
            Unavailable=false;warned=false;
        }
        catch(Exception error)
        {
            Unavailable=true;
            if(!warned){warned=true;log.Warning(error,"The running festivals could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        cancellation.Cancel();cancellation.Dispose();
    }
}
