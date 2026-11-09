using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using TenshoAssistant.Caching;

namespace TenshoAssistant.Quests;

internal sealed unsafe class QuestReader : IDisposable
{
    private readonly object gate = new();
    private readonly QuestService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IUnlockState unlocks;
    private readonly IPluginLog log;
    private readonly List<QuestEntry> entries = [];
    private ExcelSheet<Quest>? quests;
    private SubrowExcelSheet<QuestClassJobReward>? rewardJobs;
    private ExcelSheet<BeastRankBonus>? rewardBonuses;
    private IReadOnlyDictionary<uint,uint?>? rewardTomestones;
    private bool rewardReadUnavailable,rewardErrorLogged;
    private string? rewardDataVersion;
    private int position;
    private ulong character;
    private bool scanning = true;
    private bool disposed;
    private long nextRefresh;
    private readonly Func<TimeSpan> cacheLifetime;
    private readonly DataCache? disk;
    private string? cacheVersion;
    // 0.283: entries carry the journal section; older cached quest lists are read again.
    private const string QuestCacheShape="|quest-locks-286";
    private Task<CachedValue<QuestCacheData>?>? restore;
    public QuestReader(QuestService service, IDataManager data, IFramework framework,
        IPlayerState player, IClientState client, IUnlockState unlocks, IPluginLog log,Func<TimeSpan>? cacheLifetime=null,DataCache? disk=null)
    {
        this.service = service; this.data = data; this.framework = framework;
        this.player = player; this.client = client; this.unlocks = unlocks; this.log = log;
        this.cacheLifetime=cacheLifetime ?? (()=>TimeSpan.FromMinutes(5));this.disk=disk;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
        try{client.Logout+=OnLogout;}catch{TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);throw;}
    }
    private void OnLogout(int type,int code)
    {
        // 0.331: the kept catalogue is published again without character (no progress, not loading, reward index rebuilt); left
        // invalidated, it stayed « loading » with no reward index until the next login, nothing rescanning without a character.
        lock(gate)
        {
            character=0;entries.Clear();position=0;restore=null;nextRefresh=0;service.Invalidate();
            if(service.State.Entries.Count>0)service.Publish(0,service.State.Entries);
            scanning=service.State.Entries.Count==0;
        }
    }
    private void OnUpdate(IFramework unused)
    {
        lock (gate)
        {
            if (disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            var owner = client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
            var now = Stopwatch.GetTimestamp();
            var force=service.ConsumeRefresh();
            if (owner != character || force || (!scanning && restore is null && owner!=0 && now >= nextRefresh && !CacheAge.Fresh(service.State.ObservedAt,DateTimeOffset.UtcNow,cacheLifetime())))
            {
                character = owner; position = 0; entries.Clear(); scanning = true;
                rewardReadUnavailable=false;rewardErrorLogged=false;
                service.Invalidate();
                if(force && owner!=0 && disk is not null)_=disk.InvalidateAsync("quests",owner.ToString(),cacheVersion ??= GameCacheIdentity.For(data)+QuestCacheShape);
                restore=!force && owner!=0 && disk is not null ? disk.ReadAsync<QuestCacheData>("quests",owner.ToString(),cacheVersion ??= GameCacheIdentity.For(data)+QuestCacheShape):null;
                scanning=restore is null;
            }
            if(restore is { } loading)
            {
                if(!loading.IsCompleted)return;
                var saved=loading.GetAwaiter().GetResult();restore=null;
                if(saved is not null && CacheSnapshots.Valid(saved.Value,owner)
                    && CacheAge.Fresh(saved.RecordedAt,DateTimeOffset.UtcNow,cacheLifetime()))
                {
                    try{service.Publish(owner,saved.Value.Entries,saved.RecordedAt,true);scanning=false;nextRefresh=0;return;}
                    catch(Exception error){log.Warning(error,"Cached quest snapshot rejected; reading game data.");}
                }
                scanning=true;
            }
            if (!scanning) return;
            try
            {
                quests ??= data.GetExcelSheet<Quest>();
                var end = Math.Min(position + 32, quests.Count);
                var start = Stopwatch.GetTimestamp();
                while (position < end)
                {
                    var row = quests.GetRowAt(position++);
                    var name = row.Name.ExtractText();
                    if (row.RowId != 0 && !string.IsNullOrWhiteSpace(name))
                    {
                        var manager = character == 0 ? null : QuestManager.Instance();
                        var progress = character == 0 ? QuestProgress.Unconfirmed
                            : manager != null && manager->IsQuestAccepted(row.RowId) ? QuestProgress.Active
                            : unlocks.IsQuestCompleted(row) ? QuestProgress.Complete : QuestProgress.Unconfirmed;
                        byte? sequence = progress == QuestProgress.Active ? QuestManager.GetQuestSequence(row.RowId) : null;
                        var rewards=CopyRewards(row,out var rewardError);
                        var sectionName = ""; var sectionId = (byte)255;
                        if (row.JournalGenre.IsValid && row.JournalGenre.Value.JournalCategory.IsValid
                            && row.JournalGenre.Value.JournalCategory.Value.JournalSection.IsValid)
                        {
                            var section = row.JournalGenre.Value.JournalCategory.Value.JournalSection;
                            sectionName = section.Value.Name.ExtractText();
                            sectionId = section.RowId < 255 ? (byte)section.RowId : (byte)255;
                        }
                        // 0.331 (R-14): every sheet link checked before it is read; one that does not resolve gives an empty or 0 value
                        // instead of failing the whole quest catalogue.
                        entries.Add(new(row.RowId, name, row.JournalGenre.IsValid ? row.JournalGenre.Value.Name.ExtractText() : "", row.Icon, progress, sequence)
                        {
                            Section = sectionName,
                            SectionId = sectionId,
                            GenreId = row.JournalGenre.RowId,
                            Place = row.PlaceName.IsValid ? row.PlaceName.Value.Name.ExtractText() : "",
                            PreviousQuestIds = Array.AsReadOnly(row.PreviousQuest.Select(link => link.RowId).Where(id => id != 0).Distinct().ToArray()),
                            PreviousQuestJoin = row.PreviousQuestJoin,
                            LockIds = Array.AsReadOnly(row.QuestLock.Select(link => link.RowId).Where(id => id != 0).Distinct().ToArray()),
                            PreviousQuestStep = row.Unknown7,
                            IsRepeatable = row.IsRepeatable,
                            Level = row.ClassJobLevel.FirstOrDefault(),
                            AdditionalLevel = row.ClassJobLevel.Skip(1).FirstOrDefault(),
                            Zone = row.PlaceName.RowId == 0 || !row.PlaceName.IsValid ? "" : row.PlaceName.Value.Name.ExtractText(),
                            Expansion = row.Expansion.IsValid ? row.Expansion.Value.Name.ExtractText() : "",
                            IssuerId = row.IssuerStart.RowId,
                            IssuerMapId = row.IssuerLocation.RowId == 0 || !row.IssuerLocation.IsValid ? 0 : row.IssuerLocation.Value.Map.RowId,
                            IssuerTerritoryId = row.IssuerLocation.RowId == 0 || !row.IssuerLocation.IsValid ? 0 : row.IssuerLocation.Value.Territory.RowId
                            ,Rewards=rewards,RewardsError=rewardError
                        });
                    }
                    if (Stopwatch.GetElapsedTime(start) >= TimeSpan.FromMilliseconds(2)) break;
                }
                if (position != quests.Count) return;
                var after = client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
                if (after != character) { service.Invalidate(); character = after; position = 0; entries.Clear(); rewardReadUnavailable=false;rewardErrorLogged=false;return; }
                service.Publish(character, entries);
                if(character!=0 && disk is not null && service.State.ObservedAt is { } date)
                    _=disk.WriteAsync("quests",character.ToString(),cacheVersion ??= GameCacheIdentity.For(data)+QuestCacheShape,new QuestCacheData(character,service.State.Entries),date);
                entries.Clear(); scanning = false;
                nextRefresh = 0;
            }
            catch (Exception error)
            {
                service.Fail(error); entries.Clear(); scanning = false;
                nextRefresh = Stopwatch.GetTimestamp() + 30 * Stopwatch.Frequency;
                log.Warning(error, "Tensho quest read failed; retry in thirty seconds.");
            }
        }
    }
    private QuestRewardSnapshot? CopyRewards(Quest row,out string? error)
    {
        error=null;
        if(rewardReadUnavailable){error="Rewards not available in this reading; progress and other data kept.";return null;}
        try
        {
            rewardJobs ??= data.GetSubrowExcelSheet<QuestClassJobReward>();
            rewardBonuses ??= data.GetExcelSheet<BeastRankBonus>();
            rewardTomestones ??= QuestRewardCurrencyMap.Build(data.GetExcelSheet<TomestonesItem>().Select(value=>(value.Tomestones.RowId,value.Item.RowId)));
            rewardDataVersion ??= string.Join(";",data.GameData.Repositories.Values.OrderBy(repository=>repository.Name).Select(repository=>$"{repository.Name}={repository.Version}"));
        }
        catch(Exception failure)
        {
            rewardReadUnavailable=true;error="Reward tables unavailable; other quest data kept.";
            log.Warning(failure,"Quest reward tables unavailable; quest observations retained.");return null;
        }
        try{return QuestRewardCopy.FromRow(row,rewardJobs,rewardBonuses,rewardTomestones,rewardDataVersion);}
        catch(Exception failure)
        {
            error="This quest's rewards cannot be interpreted; other data kept.";
            if(!rewardErrorLogged){rewardErrorLogged=true;log.Warning(failure,"Quest reward row unavailable; other observations retained.");}
            return null;
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
            client.Logout-=OnLogout;
            service.Dispose(); entries.Clear(); quests = null;
            rewardJobs=null;rewardBonuses=null;rewardTomestones=null;rewardDataVersion=null;
        }
    }
}
