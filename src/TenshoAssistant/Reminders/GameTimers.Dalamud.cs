using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace TenshoAssistant.Reminders;

/// <summary>Counters the game's Calendar shows, copied on the framework thread (null = not available yet).</summary>
public sealed record GameTimerSnapshot(DateTimeOffset ReadAt,uint? AlliedQuestsLeft,int? LeveAllowances,DateTimeOffset? NextLeveAllowances,
    int? DeliveriesLeft,IReadOnlyList<(string Name,DateTimeOffset? Done)>? Ventures,
    SquadronTimer? SquadronMission=null,SquadronTimer? SquadronTraining=null,IReadOnlyList<RouletteDaily>? Roulettes=null,
    WondrousTails? Bingo=null,int? HuntBillsHeld=null,DateTimeOffset? NextMapAllowance=null,
    (int Done,int Total)? ChallengeLog=null,(int Donated,int Allowance)? DomanEnclave=null,IReadOnlyList<HuntBillProgress>? HuntBills=null);

/// <summary>g, M-02 (0.372): the Wondrous Tails journal (PlayerState): held or not, stickers placed, expiry, second chance points.</summary>
// N-03 (0.439): Placed = the 16 cells as bits (IsWeeklyBingoStickerPlaced, address checked); null when not read.
// N-02 (0.443): Tasks = the 16 tasks (WeeklyBingoOrderData row, status 0 open, 1 claimable, 2 claimed); null when not read.
public sealed record WondrousTails(bool Held,int Stickers,DateTimeOffset? Expires,uint SecondChance,ushort? Placed=null,IReadOnlyList<(uint Order,int Status)>? Tasks=null);

/// <summary>A daily duty roulette: done today (null = not read) and the role that gets the bonus (null = none or not known).</summary>
public sealed record RouletteDaily(byte Id,bool? Completed,RouletteRole? Bonus);

public enum RouletteRole : byte { Tank, Healer, Dps }

/// <summary>A daily roulette of the game data: its ContentRoulette row and its ContentRouletteRoleBonus row (0 = no role bonus).</summary>
public readonly record struct RouletteRow(byte Id,byte BonusRow);

/// <summary>A squadron mission or training: its row (0 = none running) and the end the game holds (null = not given).</summary>
public sealed record SquadronTimer(ushort Id,DateTimeOffset? Done);

/// <summary>
/// R-08 (0.300): reads the counters behind the game's Calendar — allied society quests left today (QuestManager), leve allowances
/// and their next refill (QuestManager), custom deliveries left this week (SatisfactionSupplyManager), retainer ventures
/// (RetainerManager, known once the retainer list was opened at a summoning bell). Framework thread only; values are copied.
/// </summary>
public static unsafe class GameTimers
{
    /// <summary>g, M-02 (0.372): whether a roulette was done today, for the Duty Finder panel (null = not read); framework thread.</summary>
    public static bool? RouletteDone(byte roulette)
    {
        try
        {
            var content=InstanceContent.Instance();
            return content!=null && (nint)InstanceContent.MemberFunctionPointers.IsRouletteIncomplete!=0 ? !content->IsRouletteIncomplete(roulette):null;
        }
        catch(Exception){return null;}
    }

    public static GameTimerSnapshot Read(IReadOnlyList<RouletteRow>? roulettes=null,IReadOnlyList<int>? challengeRows=null)
    {
        // R-14 (0.453): before the character is loaded (title screen, character select) the game answers zeros (role bonus « Tank », every
        // roulette open): unknown instead. Each counter guarded on its own: one signature lost after an update dropped every reminder.
        var characterLoaded=false;
        try{var loadedState=PlayerState.Instance();characterLoaded=loadedState!=null && loadedState->IsLoaded;}
        catch(Exception){characterLoaded=false;}
        uint? allied=null;int? leves=null;DateTimeOffset? nextLeves=null;
        if(characterLoaded)
            try
            {
                var quests=QuestManager.Instance();
                if(quests!=null)
                {
                    allied=quests->GetBeastTribeAllowance();
                    leves=quests->NumLeveAllowances;
                    var at=QuestManager.GetNextLeveAllowancesUnixTimestamp();
                    if(at>0)nextLeves=DateTimeOffset.FromUnixTimeSeconds(at);
                }
            }
            catch(Exception){allied=null;leves=null;nextLeves=null;}
        int? deliveries=null;
        if(characterLoaded)
            try
            {
                var supply=SatisfactionSupplyManager.Instance();
                deliveries=supply!=null ? supply->GetRemainingAllowances():null;
            }
            catch(Exception){deliveries=null;}
        List<(string,DateTimeOffset?)>? ventures=null;
        try
        {
            var retainers=RetainerManager.Instance();
            if(retainers!=null && retainers->IsReady)
            {
                ventures=[];
                for(var index=0u;index<retainers->GetRetainerCount();index++)
                {
                    var retainer=retainers->GetRetainerBySortedIndex(index);
                    if(retainer==null || retainer->RetainerId==0 || retainer->VentureId==0)continue;
                    ventures.Add((retainer->NameString,retainer->VentureComplete>0 ? DateTimeOffset.FromUnixTimeSeconds(retainer->VentureComplete):null));
                }
            }
        }
        catch(Exception){ventures=null;}
        // 0.333 (DEC-023 6, user request « mission and training »): the squadron's mission and training ends, held by PlayerState for
        // the whole session (docs/evidence/0.332-squadron-probe.md). Guarded on its own: a failure leaves them unknown, the rest stays.
        SquadronTimer? mission=null,training=null;
        try
        {
            var player=PlayerState.Instance();
            if(player!=null && player->IsLoaded)
            {
                mission=new(player->ActiveGcArmyExpedition,Unix(player->SquadronMissionCompletionTimestamp));
                training=new(player->ActiveGcArmyTraining,Unix(player->SquadronTrainingCompletionTimestamp));
            }
        }
        catch(Exception){mission=null;training=null;}
        // g, C-02 (0.356): daily roulettes done today (InstanceContent.IsRouletteIncomplete, negated, by ContentRoulette row, called only once its
        // address is resolved) and the role bonus the Duty Finder holds (AgentContentsFinder.ContentRouletteRoleBonuses, 11 entries,
        // indexed by the ContentRouletteRoleBonus row — rows 0 to 10, docs/evidence/0.356-roulettes.md). Guarded on its own.
        List<RouletteDaily>? daily=null;
        if(characterLoaded && roulettes is { Count:>0 })
            try
            {
                var content=InstanceContent.Instance();
                var canAsk=content!=null && (nint)InstanceContent.MemberFunctionPointers.IsRouletteIncomplete!=0;
                var agent=AgentContentsFinder.Instance();
                var bonuses=agent!=null ? agent->ContentRouletteRoleBonuses:default;
                daily=[];
                foreach(var roulette in roulettes)
                {
                    RouletteRole? bonus=roulette.BonusRow!=0 && roulette.BonusRow<bonuses.Length && bonuses[roulette.BonusRow] is var role
                        && role is ContentsRouletteRole.Tank or ContentsRouletteRole.Healer or ContentsRouletteRole.Dps ? (RouletteRole)(byte)role:null;
                    daily.Add(new(roulette.Id,canAsk ? !content->IsRouletteIncomplete(roulette.Id):null,bonus));
                }
            }
            catch(Exception){daily=null;}
        // g, M-02 (0.372): Wondrous Tails (PlayerState, memory), hunt bills held (MobHunt.ObtainedFlags, one bit per mark bill) and the
        // next treasure map allowance (UIState.GetNextMapAllowanceTimestamp, address checked). Each guarded on its own.
        WondrousTails? bingo=null;int? bills=null;DateTimeOffset? nextMap=null;IReadOnlyList<HuntBillProgress>? huntBills=null;
        try
        {
            var state=PlayerState.Instance();
            if(state!=null && state->IsLoaded)
            {
                ushort? placed=null;
                if(state->HasWeeklyBingoJournal && (nint)PlayerState.MemberFunctionPointers.IsWeeklyBingoStickerPlaced!=0)
                {
                    ushort cells=0;
                    for(var cell=0;cell<16;cell++)if(state->IsWeeklyBingoStickerPlaced(cell))cells|=(ushort)(1<<cell);
                    placed=cells;
                }
                List<(uint,int)>? tasks=null;
                if(state->HasWeeklyBingoJournal && (nint)PlayerState.MemberFunctionPointers.GetWeeklyBingoTaskStatus!=0)
                {
                    tasks=[];var orders=state->WeeklyBingoOrderData;
                    for(var task=0;task<16 && task<orders.Length;task++)tasks.Add((orders[task],(int)state->GetWeeklyBingoTaskStatus(task)));
                }
                bingo=state->HasWeeklyBingoJournal
                    ? new(true,state->WeeklyBingoNumPlacedStickers,Unix(state->GetWeeklyBingoExpireUnixTimestamp()),state->WeeklyBingoNumSecondChancePoints,placed,tasks)
                    :new(false,0,null,0);
            }
        }
        catch(Exception){bingo=null;}
        try
        {
            var hunt=MobHunt.Instance();
            if(hunt!=null)bills=System.Numerics.BitOperations.PopCount((uint)hunt->ObtainedFlags & ((1u<<MobHunt.MaxMarkIndex)-1));
            // N-01 (0.440): each held bill's order (GetObtainedHuntOrderRowId, address checked) and its targets' kills (CurrentKills).
            if(hunt!=null && (nint)MobHunt.MemberFunctionPointers.GetObtainedHuntOrderRowId!=0)
            {
                var held=new List<HuntBillProgress>();
                for(var index=0;index<MobHunt.MaxMarkIndex;index++)
                {
                    if((hunt->ObtainedFlags & (1<<index))==0)continue;
                    var row=hunt->GetObtainedHuntOrderRowId((byte)index);
                    if(row<=0)continue;
                    var counts=hunt->CurrentKills[index].Counts;
                    held.Add(new((byte)index,(uint)row,counts.ToArray()));
                }
                huntBills=held;
            }
        }
        catch(Exception){bills=null;huntBills=null;}
        try
        {
            var ui=UIState.Instance();
            if(ui!=null && (nint)UIState.MemberFunctionPointers.GetNextMapAllowanceTimestamp!=0 && ui->GetNextMapAllowanceTimestamp() is > 0 and var at)
                nextMap=DateTimeOffset.FromUnixTimeSeconds(at);
        }
        catch(Exception){nextMap=null;}
        // g, M-11 (0.382): the challenge log (UIState.ContentsNote, completion bits, only once the game has loaded it: State Loaded;
        // index = ContentsNote row, to confirm in game) and the Doman Enclave donations of the week (DomanEnclaveManager; Allowance 0 =
        // not read yet). Guarded each on its own; no game function is called.
        (int,int)? challenge=null,doman=null;
        try
        {
            var ui=UIState.Instance();
            if(ui!=null && challengeRows is { Count:>0 } rows && ui->ContentsNote.State==ContentsNote.ContentsNoteState.Loaded)
                challenge=(rows.Count(row=>ui->ContentsNote.IsContentNoteComplete(row)),rows.Count);
        }
        catch(Exception){challenge=null;}
        try
        {
            var enclave=DomanEnclaveManager.Instance();
            if(enclave!=null && enclave->State.Allowance>0)doman=(enclave->State.Donated,enclave->State.Allowance);
        }
        catch(Exception){doman=null;}
        return new(DateTimeOffset.UtcNow,allied,leves,nextLeves,deliveries,ventures,mission,training,daily,bingo,bills,nextMap,challenge,doman,huntBills);
        static DateTimeOffset? Unix(int seconds)=>seconds>0 ? DateTimeOffset.FromUnixTimeSeconds(seconds):null;
    }
}
