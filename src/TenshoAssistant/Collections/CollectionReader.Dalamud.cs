using System.Diagnostics;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using TenshoAssistant.Caching;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant.Collections;

internal sealed partial class CollectionReader : IDisposable
{
    private readonly object gate = new();
    private readonly CollectionService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IUnlockState unlocks;
    private readonly IPluginLog log;
    private ExcelSheet<Mount>? mounts;
    private ExcelSheet<Companion>? companions;
    private ExcelSheet<Emote>? emotes;
    private ExcelSheet<AozAction>? blue;
    private ExcelSheet<AozActionTransient>? blueDetails;
    private ExcelSheet<TripleTriadCard>? cards;
    private ExcelSheet<TripleTriadCardResident>? cardDetails;
    private ExcelSheet<Orchestrion>? orchestrion;
    private ExcelSheet<OrchestrionUiparam>? orchestrionUi;
    private ExcelSheet<OrchestrionCategory>? orchestrionCategories;
    private bool orchestrionUnavailable,orchestrionUnlockUnavailable;
    private string? orchestrionIssue;
    private readonly List<CollectionEntry> entries = [];
    private int position;
    private ulong character;
    private bool scanning = true;
    private bool disposed;
    private long nextRefresh;
    private readonly Func<TimeSpan> cacheLifetime;
    private readonly Func<TimeSpan> completionLifetime;
    private readonly DataCache? disk;
    private string? cacheVersion;
    private Task<CachedValue<CollectionCacheData>?>? restore;

    public CollectionReader(CollectionService service, IDataManager data, IFramework framework,
        IPlayerState player, IClientState client, IUnlockState unlocks, IPluginLog log,Func<TimeSpan>? cacheLifetime=null,DataCache? disk=null,
        Func<TimeSpan>? completionLifetime=null)
    {
        this.service = service; this.data = data; this.framework = framework;
        this.player = player; this.client = client; this.unlocks = unlocks; this.log = log;
        this.cacheLifetime=cacheLifetime ?? (()=>TimeSpan.FromDays(1));this.disk=disk;this.completionLifetime=completionLifetime ?? (()=>TimeSpan.FromDays(7));
        unlocks.Unlock+=OnCollectionUnlock;
        try{framework.Update += OnUpdate;try{client.Logout+=OnLogout;}catch{framework.Update-=OnUpdate;throw;}}
        catch{unlocks.Unlock-=OnCollectionUnlock;throw;}
    }
    private void OnCollectionUnlock(RowRef row)
    {
        var type=row.RowType;
        if(type==typeof(Title) || type==typeof(Achievement) || type==typeof(Orchestrion) || type==typeof(Mount)
            || type==typeof(Companion) || type==typeof(Emote) || type==typeof(AozAction) || type==typeof(TripleTriadCard))service.RequestRefresh();
    }
    private void OnUpdate(IFramework _)
    {
        lock (gate) { ReadUpdate(); }
    }
    private void OnLogout(int type,int code)
    {
        lock(gate){character=0;entries.Clear();position=0;restore=null;nextRefresh=0;ResetCompletion();service.Invalidate();scanning=service.State.Entries.Count==0;}
    }
    private void ReadUpdate()
    {
        if (disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        var owner = client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
        var now = Stopwatch.GetTimestamp();
        if(!scanning && owner!=0 && CompletionLoadingChanged())service.RequestRefresh();
        try{service.SetListsLoaded(owner==0 ? null:unlocks.IsTitleListLoaded,owner==0 ? null:unlocks.IsAchievementListLoaded);}
        catch{service.SetListsLoaded(null,null);}
        var force=service.ConsumeRefresh();
        if (owner != character || force || (!scanning && restore is null && owner!=0 && now >= nextRefresh && !CacheAge.Fresh(service.State.ObservedAt,DateTimeOffset.UtcNow,cacheLifetime())))
        {
            character = owner;
            scanning = true;
            position = 0;
            entries.Clear();
            orchestrionUnavailable=false;orchestrionUnlockUnavailable=false;orchestrionIssue=null;
            ResetCompletion();
            service.Invalidate();
            if(force && owner!=0 && disk is not null)_=disk.InvalidateAsync("collections",owner.ToString(),cacheVersion ??= GameCacheIdentity.For(data));
            restore=!force && owner!=0 && disk is not null ? disk.ReadAsync<CollectionCacheData>("collections",owner.ToString(),cacheVersion ??= GameCacheIdentity.For(data)):null;
            scanning=restore is null;
        }
        if(restore is { } loading)
        {
            if(!loading.IsCompleted)return;
            var saved=loading.GetAwaiter().GetResult();restore=null;
            // Even a stale copy keeps its title/achievement states for the next read (CompletionMemory).
            if(saved is not null && CacheSnapshots.Valid(saved.Value,owner))
                RememberCompletion(owner,saved.Value.Entries,saved.Value.TitlesCheckedAt,saved.Value.AchievementsCheckedAt);
            if(saved is not null && CacheSnapshots.Valid(saved.Value,owner)
                && CacheAge.Fresh(saved.RecordedAt,DateTimeOffset.UtcNow,cacheLifetime()))
            {
                try
                {
                    service.Publish(owner,saved.Value.Entries,saved.Value.Issues,saved.RecordedAt,true);
                    lastTitlesLoaded=unlocks.IsTitleListLoaded;lastAchievementsLoaded=unlocks.IsAchievementListLoaded;
                    scanning=false;nextRefresh=0;return;
                }
                catch(Exception error){log.Warning(error,"Cached collections rejected; reading game data.");}
            }
            scanning=true;
        }
        if (!scanning) return;
        try
        {
            mounts ??= data.GetExcelSheet<Mount>();
            companions ??= data.GetExcelSheet<Companion>();
            emotes ??= data.GetExcelSheet<Emote>();
            blue ??= data.GetExcelSheet<AozAction>();
            blueDetails ??= data.GetExcelSheet<AozActionTransient>();
            cards ??= data.GetExcelSheet<TripleTriadCard>();
            cardDetails ??= data.GetExcelSheet<TripleTriadCardResident>();
            if(!orchestrionUnavailable)
            {
                try
                {
                    orchestrion ??= data.GetExcelSheet<Orchestrion>();
                    orchestrionUi ??= data.GetExcelSheet<OrchestrionUiparam>();
                    orchestrionCategories ??= data.GetExcelSheet<OrchestrionCategory>();
                }
                catch(Exception error)
                {
                    orchestrionUnavailable=true;orchestrionIssue="Catalogue d’orchestrion indisponible ; les autres collections restent consultables.";
                    log.Warning(error,"Orchestrion sheets unavailable; other collection categories retained.");
                }
            }
            var musicStart=mounts.Count+companions.Count+emotes.Count+blue.Count+cards.Count;
            PrepareCompletion();
            var titleStart=musicStart+(orchestrionUnavailable ? 0:orchestrion!.Count);
            var achievementStart=titleStart+TitleCount;
            var total=achievementStart+AchievementCount;
            var end = Math.Min(position + 32, total);
            var start = Stopwatch.GetTimestamp();
            while (position < end)
            {
                if (position < mounts.Count)
                {
                    var row = mounts.GetRowAt(position++);
                    var name = row.Singular.ExtractText();
                    if (row.RowId != 0 && !string.IsNullOrWhiteSpace(name))
                        entries.Add(new(CollectionKind.Mount, row.RowId, name, row.Icon, character != 0 && unlocks.IsMountUnlocked(row)));
                }
                else if (position < mounts.Count + companions.Count)
                {
                    var row = companions.GetRowAt(position++ - mounts.Count);
                    var name = row.Singular.ExtractText();
                    if (row.RowId != 0 && !string.IsNullOrWhiteSpace(name))
                        entries.Add(new(CollectionKind.Companion, row.RowId, name, row.Icon, character != 0 && unlocks.IsCompanionUnlocked(row)));
                }
                else if (position < mounts.Count + companions.Count + emotes.Count)
                {
                    var row = emotes.GetRowAt(position++ - mounts.Count - companions.Count);
                    var name = row.Name.ExtractText();
                    if (row.RowId != 0 && !string.IsNullOrWhiteSpace(name))
                        entries.Add(new(CollectionKind.Emote, row.RowId, name, row.Icon, character != 0 && unlocks.IsEmoteUnlocked(row)));
                }
                else if(position>=achievementStart)
                {
                    if(!ReadAchievement(position++-achievementStart))position=total;
                }
                else if(position>=titleStart)
                {
                    if(!ReadTitle(position++-titleStart))position=achievementStart;
                }
                else if(position>=musicStart)
                {
                    try
                    {
                        var row=orchestrion!.GetRowAt(position++-musicStart);
                        OrchestrionUiparam? detail=orchestrionUi!.TryGetRow(row.RowId,out var found) ? found:null;
                        OrchestrionCategory? category=detail is { } info && orchestrionCategories!.TryGetRow(info.OrchestrionCategory.RowId,out var group) ? group:null;
                        var copied=OrchestrionCopy.FromRows(row,detail,category,false);
                        var confirmed=false;
                        if(copied is not null && character!=0 && !orchestrionUnlockUnavailable)
                        {
                            try{confirmed=unlocks.IsOrchestrionUnlocked(row);}
                            catch(Exception error)
                            {
                                orchestrionUnlockUnavailable=true;orchestrionIssue="Acquisitions d’orchestrion partiellement lues ; les résultats non confirmés restent inconnus.";
                                log.Warning(error,"Orchestrion acquisitions unavailable; static catalogue retained.");
                            }
                        }
                        if(copied is not null)entries.Add(copied with {ConfirmedUnlocked=confirmed});
                    }
                    catch(Exception error)
                    {
                        position=titleStart;
                        orchestrionIssue="Catalogue d’orchestrion partiellement lu ; les autres collections restent consultables.";
                        log.Warning(error,"Orchestrion row unavailable; other collection categories retained.");
                    }
                }
                else if (position >= mounts.Count + companions.Count + emotes.Count + blue.Count)
                {
                    var row = cards.GetRowAt(position++ - mounts.Count - companions.Count - emotes.Count - blue.Count);
                    var name = row.Name.ExtractText();
                    if (row.RowId != 0 && !string.IsNullOrWhiteSpace(name) && cardDetails.TryGetRow(row.RowId, out var details))
                    {
                        var card = new TriadCardDefinition(row.RowId, details.Order, details.TripleTriadCardRarity.Value.Stars,
                            details.Top, details.Right, details.Bottom, details.Left, details.TripleTriadCardType.RowId,
                            details.TripleTriadCardType.Value.Name.ExtractText(), row.Description.ExtractText());
                        if (card.IsValid)
                            entries.Add(new(CollectionKind.TripleTriad, row.RowId, name, card.IconId,
                                character != 0 && unlocks.IsTripleTriadCardUnlocked(row), Card: card));
                    }
                }
                else
                {
                    var row = blue.GetRowAt(position++ - mounts.Count - companions.Count - emotes.Count);
                    if (row.RowId != 0 && row.Action.RowId != 0)
                    {
                        var action = row.Action.Value;
                        var name = action.Name.ExtractText();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            var number = 0;
                            var description = "";
                            var location = "";
                            if (blueDetails.TryGetRow(row.RowId, out var details))
                            {
                                number = details.Number;
                                description = details.Description.ExtractText();
                                var source = details.Location;
                                if (source.RowId != 0)
                                {
                                    if (source.RowType == typeof(PlaceName)) location = data.GetExcelSheet<PlaceName>().GetRow(source.RowId).Name.ExtractText();
                                    else if (source.RowType == typeof(TerritoryType)) location = data.GetExcelSheet<TerritoryType>().GetRow(source.RowId).PlaceName.Value.Name.ExtractText();
                                    else if (source.RowType == typeof(ContentFinderCondition)) location = data.GetExcelSheet<ContentFinderCondition>().GetRow(source.RowId).Name.ExtractText();
                                }
                            }
                            entries.Add(new(CollectionKind.BlueMagic, row.RowId, name, action.Icon,
                                character != 0 && unlocks.IsAozActionUnlocked(row), number, row.Rank, description, location));
                        }
                    }
                }
                if (Stopwatch.GetElapsedTime(start) >= TimeSpan.FromMilliseconds(2)) break;
            }
            if (position != total) return;
            _=FinalizeCompletion();
            if(service.ConsumeRefresh())
            {
                position=0;entries.Clear();orchestrionUnavailable=false;orchestrionUnlockUnavailable=false;orchestrionIssue=null;
                ResetCompletion();service.Invalidate();return;
            }
            var after = client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
            if (after != character) { service.Invalidate(); character = after; position = 0; entries.Clear(); orchestrionUnavailable=false;orchestrionUnlockUnavailable=false;orchestrionIssue=null; ResetCompletion();return; }
            var issues=new List<CollectionCategoryIssue>();
            if(orchestrionIssue is not null)issues.Add(new(CollectionKind.Orchestrion,orchestrionIssue));
            issues.AddRange(CompletionIssues());
            if(character!=0)ApplyCompletionMemory(character,entries,completionLifetime());
            service.Publish(character,entries,issues);
            if(character!=0 && disk is not null && service.State.ObservedAt is { } date)
                _=disk.WriteAsync("collections",character.ToString(),cacheVersion ??= GameCacheIdentity.For(data),
                    new CollectionCacheData(character,service.State.Entries,service.State.CategoryIssues,titlesCheckedAt,achievementsCheckedAt),date);
            entries.Clear();
            scanning = false;
            nextRefresh = 0;
        }
        catch (Exception error)
        {
            service.Fail(error);
            entries.Clear();
            scanning = false;
            nextRefresh = Stopwatch.GetTimestamp() + 30 * Stopwatch.Frequency;
            log.Warning(error, "Tensho collections read failed; retry in thirty seconds.");
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
        if (disposed) return;
        disposed = true;
        framework.Update -= OnUpdate;
        client.Logout-=OnLogout;
        unlocks.Unlock-=OnCollectionUnlock;
        service.Clear();
        entries.Clear(); mounts = null; companions = null; emotes = null; blue = null; blueDetails = null;
        cards = null; cardDetails = null;
        orchestrion=null;orchestrionUi=null;orchestrionCategories=null;orchestrionIssue=null;
        ReleaseCompletion();
        }
    }
}
