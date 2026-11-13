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
    // X-219 (0.1229): named mounts per riding music row, counted once.
    private Dictionary<uint,int>? mountMusic;
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
    private string? orchestrionIssue,blueIssue,triadIssue;
    private readonly List<CollectionEntry> entries = [];
    private readonly RegistrationScope registrations = new();
    private int position;
    private ulong character;
    private bool scanning = true;
    private bool disposed;
    private long nextRefresh;
    private readonly Func<TimeSpan> cacheLifetime;
    private readonly Func<TimeSpan> completionLifetime;
    private readonly DataCache? disk;
    private string? cacheVersion;
    // 0.278: cards now carry « Extra » (Ex. series); older cached collections are read again instead of losing it.
    private const string CacheShape="|triad-ex-278";
    private Task<CachedValue<CollectionCacheData>?>? restore;

    public CollectionReader(CollectionService service, IDataManager data, IFramework framework,
        IPlayerState player, IClientState client, IUnlockState unlocks, IPluginLog log,Func<TimeSpan>? cacheLifetime=null,DataCache? disk=null,
        Func<TimeSpan>? completionLifetime=null)
    {
        this.service = service; this.data = data; this.framework = framework;
        this.player = player; this.client = client; this.unlocks = unlocks; this.log = log;
        this.cacheLifetime=cacheLifetime ?? (()=>TimeSpan.FromDays(1));this.disk=disk;this.completionLifetime=completionLifetime ?? (()=>TimeSpan.FromDays(7));
        // X-76 H10 (0.924): subscriptions owned by one RegistrationScope: a failure part-way undoes the ones already made; Dispose releases them in reverse order.
        try
        {
            registrations.Register(()=>unlocks.Unlock+=OnCollectionUnlock,()=>unlocks.Unlock-=OnCollectionUnlock);
            registrations.Register(()=>TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate),()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate));
            registrations.Register(()=>client.Logout+=OnLogout,()=>client.Logout-=OnLogout);
        }
        catch{registrations.Dispose();throw;}
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
        // 0.331: the kept catalogue is published again without character (no acquisition evidence, not loading); left invalidated, it
        // stayed « loading » until the next login, nothing rescanning without a character.
        lock(gate)
        {
            character=0;entries.Clear();position=0;restore=null;nextRefresh=0;ResetCompletion();service.Invalidate();
            if(service.State.Entries.Count>0)service.Publish(0,service.State.Entries);
            scanning=service.State.Entries.Count==0;
        }
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
            orchestrionUnavailable=false;orchestrionUnlockUnavailable=false;orchestrionIssue=null;blueIssue=null;triadIssue=null;
            ResetCompletion();
            service.Invalidate();
            if(force && owner!=0 && disk is not null)_=disk.InvalidateAsync("collections",owner.ToString(),cacheVersion ??= GameCacheIdentity.For(data)+CacheShape);
            restore=!force && owner!=0 && disk is not null ? disk.ReadAsync<CollectionCacheData>("collections",owner.ToString(),cacheVersion ??= GameCacheIdentity.For(data)+CacheShape):null;
            scanning=restore is null;
        }
        if(restore is { } loading)
        {
            if(!loading.IsCompleted)return;
            var saved=loading.GetAwaiter().GetResult();restore=null;
            // Even a stale copy keeps its title/achievement states for the next read (CompletionMemory).
            if(saved is not null && CacheSnapshots.Valid(saved.Value,owner))
                RememberCompletion(owner,saved.Value.Entries,saved.Value.TitlesCheckedAt,saved.Value.AchievementsCheckedAt);
            // A copy made before 0.180 lacks the achievements' English names: read again (its states stay remembered above).
            if(saved is not null && CacheSnapshots.Valid(saved.Value,owner)
                && CacheAge.Fresh(saved.RecordedAt,DateTimeOffset.UtcNow,cacheLifetime())
                && (!saved.Value.Entries.Any(entry=>entry.Kind==CollectionKind.Achievement) || saved.Value.Entries.Any(entry=>entry.Achievement is { EnglishName.Length:>0 })))
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
                    orchestrionUnavailable=true;orchestrionIssue="Orchestrion catalogue unavailable; the other collections can still be browsed.";
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
                        entries.Add(new(CollectionKind.Mount, row.RowId, name, row.Icon, character != 0 && unlocks.IsMountUnlocked(row),Mount:MountDetails(row)));
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
                                orchestrionUnlockUnavailable=true;orchestrionIssue="Orchestrion acquisitions partly read; unconfirmed results stay unknown.";
                                log.Warning(error,"Orchestrion acquisitions unavailable; static catalogue retained.");
                            }
                        }
                        if(copied is not null)entries.Add(copied with {ConfirmedUnlocked=confirmed});
                    }
                    catch(Exception error)
                    {
                        position=titleStart;
                        orchestrionIssue="Orchestrion catalogue partly read; the other collections can still be browsed.";
                        log.Warning(error,"Orchestrion row unavailable; other collection categories retained.");
                    }
                }
                else if (position >= mounts.Count + companions.Count + emotes.Count + blue.Count)
                {
                    var row = cards.GetRowAt(position++ - mounts.Count - companions.Count - emotes.Count - blue.Count);
                    var name = row.Name.ExtractText();
                    if (row.RowId != 0 && !string.IsNullOrWhiteSpace(name) && cardDetails.TryGetRow(row.RowId, out var details))
                    {
                        // 0.331 (R-14): a rarity or type link that does not resolve skips this card (said as a category issue) instead
                        // of failing every collection category, as the orchestrion rows already do.
                        // 0.454 (R-14): IsValid itself throws when the linked sheet no longer loads: guarded the same way.
                        var linked = false;
                        try { linked = details.TripleTriadCardRarity.IsValid && details.TripleTriadCardType.IsValid; }
                        catch (Exception) { linked = false; }
                        if (!linked)
                            triadIssue = "Triple Triad card catalogue partly read; the other collections can still be browsed.";
                        else
                        {
                            var card = new TriadCardDefinition(row.RowId, details.Order, details.TripleTriadCardRarity.Value.Stars,
                                details.Top, details.Right, details.Bottom, details.Left, details.TripleTriadCardType.RowId,
                                details.TripleTriadCardType.Value.Name.ExtractText(), row.Description.ExtractText(),
                                // UIPriority ≠ 0 = « Ex. » card, numbered apart (docs/evidence/0.278-triad-ex-cards.md).
                                details.UIPriority != 0);
                            if (card.IsValid)
                                entries.Add(new(CollectionKind.TripleTriad, row.RowId, name, card.IconId,
                                    character != 0 && unlocks.IsTripleTriadCardUnlocked(row), Card: card));
                        }
                    }
                }
                else
                {
                    var row = blue.GetRowAt(position++ - mounts.Count - companions.Count - emotes.Count);
                    // 0.331 (R-14): an action link that does not resolve skips this spell (said as a category issue), and a location that
                    // does not resolve stays empty (BlueSpell.MissingLocation), instead of failing every collection category.
                    if (row.RowId != 0 && row.Action.RowId != 0 && !row.Action.IsValid)
                        blueIssue = "Blue magic spell catalogue partly read; the other collections can still be browsed.";
                    else if (row.RowId != 0 && row.Action.RowId != 0)
                    {
                        var action = row.Action.Value;
                        var name = action.Name.ExtractText();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            var number = 0;
                            var description = "";
                            var location = "";
                            var dutyLevel = 0;
                            if (blueDetails.TryGetRow(row.RowId, out var details))
                            {
                                number = details.Number;
                                description = details.Description.ExtractText();
                                var source = details.Location;
                                // 0.454 (R-14): a place sheet that no longer loads leaves this location empty, as a missing row does.
                                if (source.RowId != 0)
                                    try
                                {
                                    if (source.RowType == typeof(PlaceName))
                                        location = data.GetExcelSheet<PlaceName>().TryGetRow(source.RowId, out var place) ? place.Name.ExtractText() : "";
                                    else if (source.RowType == typeof(TerritoryType))
                                        location = data.GetExcelSheet<TerritoryType>().TryGetRow(source.RowId, out var territory) ? territory.PlaceName.ValueNullable?.Name.ExtractText() ?? "" : "";
                                    else if (source.RowType == typeof(ContentFinderCondition))
                                    {
                                        // X-76 P5 (0.930): the duty's required level, from the same row (ContentFinderCondition.ClassJobLevelRequired).
                                        if (data.GetExcelSheet<ContentFinderCondition>().TryGetRow(source.RowId, out var content)) { location = content.Name.ExtractText(); dutyLevel = content.ClassJobLevelRequired; }
                                    }
                                }
                                    catch (Exception) { location = ""; blueIssue = "Blue magic spell catalogue partly read; the other collections can still be browsed."; }
                            }
                            entries.Add(new(CollectionKind.BlueMagic, row.RowId, name, action.Icon,
                                character != 0 && unlocks.IsAozActionUnlocked(row), number, row.Rank, description, location, DutyLevel: dutyLevel));
                        }
                    }
                }
                if (Stopwatch.GetElapsedTime(start) >= TimeSpan.FromMilliseconds(2)) break;
            }
            if (position != total) return;
            // 0.331: true when the title or achievement list arrived during this pass (pass unstable): read again once published below,
            // since CompletionLoadingChanged no longer sees the change.
            var again=FinalizeCompletion();
            if(service.ConsumeRefresh())
            {
                position=0;entries.Clear();orchestrionUnavailable=false;orchestrionUnlockUnavailable=false;orchestrionIssue=null;blueIssue=null;triadIssue=null;
                ResetCompletion();service.Invalidate();return;
            }
            var after = client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
            if (after != character) { service.Invalidate(); character = after; position = 0; entries.Clear(); orchestrionUnavailable=false;orchestrionUnlockUnavailable=false;orchestrionIssue=null;blueIssue=null;triadIssue=null; ResetCompletion();return; }
            var issues=new List<CollectionCategoryIssue>();
            if(orchestrionIssue is not null)issues.Add(new(CollectionKind.Orchestrion,orchestrionIssue));
            if(blueIssue is not null)issues.Add(new(CollectionKind.BlueMagic,blueIssue));
            if(triadIssue is not null)issues.Add(new(CollectionKind.TripleTriad,triadIssue));
            issues.AddRange(CompletionIssues());
            if(character!=0)ApplyCompletionMemory(character,entries,completionLifetime());
            service.Publish(character,entries,issues);
            if(character!=0 && disk is not null && service.State.ObservedAt is { } date)
                _=disk.WriteAsync("collections",character.ToString(),cacheVersion ??= GameCacheIdentity.For(data)+CacheShape,
                    new CollectionCacheData(character,service.State.Entries,service.State.CategoryIssues,titlesCheckedAt,achievementsCheckedAt),date);
            entries.Clear();
            scanning = false;
            nextRefresh = 0;
            if(again)service.RequestRefresh();
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
    /// <summary>
    /// X-219 (0.1229): riders, riding music shared or not, and mount actions, from the Mount sheet (docs/evidence/0.1229-mount-sheet.md); null when
    /// a column does not read (after a game update), never a guessed value.
    /// </summary>
    private MountMetadata? MountDetails(Mount row)
    {
        try
        {
            mountMusic ??= mounts!.Where(mount=>mount.RowId!=0 && !mount.Singular.IsEmpty).GroupBy(mount=>mount.RideBGM.RowId).ToDictionary(group=>group.Key,group=>group.Count());
            var music=row.RideBGM.RowId==0 ? 0:mountMusic.GetValueOrDefault(row.RideBGM.RowId);
            var actions=row.MountAction.RowId==0 ? []:row.MountAction.ValueNullable?.Action.Where(action=>action.RowId!=0)
                .Select(action=>action.ValueNullable?.Name.ExtractText() ?? "").Where(text=>text.Length>0).Distinct().ToArray() ?? [];
            return new(row.ExtraSeats+1,music,actions);
        }
        catch(Exception error) when(error is not OutOfMemoryException){return null;}
    }

    public void Dispose()
    {
        lock (gate)
        {
        if (disposed) return;
        disposed = true;
        registrations.Dispose();
        service.Clear();
        entries.Clear(); mounts = null; companions = null; emotes = null; blue = null; blueDetails = null;
        cards = null; cardDetails = null;
        orchestrion=null;orchestrionUi=null;orchestrionCategories=null;orchestrionIssue=null;
        ReleaseCompletion();
        }
    }
}
