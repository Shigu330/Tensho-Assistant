using System.Globalization;

namespace TenshoAssistant.Gathering;

/// <summary>
/// 0.479 (user request, in-game campaign): what the game's Collectable window says for the next action, from its values (docs/evidence/0.479-collectable-window-values.md): the item, the current
/// collectability, and the gain of each action as the game shows it this turn. Null when a value is missing or of another type, so a patch that moves them gives no advice rather than a wrong one.
/// 0.481: values 54 and 56 are the game's boxes « with Scrutiny » and « with Intuition », which add those bonuses to the gains shown, whether they will happen or not. Value 58 holds Intuition's chance.
/// </summary>
public sealed record CollectableTurn(uint ItemId,int Current,int Maximum,int Scour,int BrazenMin,int BrazenMax,int Meticulous,bool EstimateScrutiny,bool EstimateIntuition,
    uint ScourAction,uint BrazenAction,uint MeticulousAction,uint CollectAction,uint ScrutinyAction,uint FocusAction,int MeticulousChance=0,int IntuitionChance=0,
    bool ScourOpen=true,bool BrazenOpen=true,bool MeticulousOpen=true,bool ScrutinyOpen=true,bool FocusOpen=true,uint PrimingAction=0,bool PrimingOpen=false)
{
    /// <summary>
    /// What a bonus adds to the gains shown. Brazen goes from 50 to 150 % of the plain gain and a bonus adds the same to everything, so its width is the plain gain. Null when the numbers do not fit that
    /// rule, as on the turn after an Intuition, when the game raises Brazen's floor.
    /// </summary>
    public int? Extra
    {
        get
        {
            var plain=BrazenMax-BrazenMin;
            if(plain<=0 || Scour<plain)return null;
            var extra=Scour-plain;
            return Math.Abs(BrazenMax-(plain*3/2+extra))<=2 ? extra:null;
        }
    }

    public static CollectableTurn? Parse(IReadOnlyList<string>? raw)
    {
        if(raw is null || raw.Count<57)return null;
        uint? Number(int index)
        {
            var parts=raw[index].Split(' ',2);
            return parts.Length==2 && parts[0]==$"{index}:uint" && uint.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out var value) ? value:null;
        }
        bool? Flag(int index)
        {
            var parts=raw[index].Split(' ',2);
            return parts.Length==2 && parts[0]==$"{index}:bool" && bool.TryParse(parts[1],out var value) ? value:null;
        }
        if(Number(2) is not { } item || item==0 || Number(13) is not { } current || Number(14) is not { } maximum || maximum is 0 or >100000 || current>maximum
            || Number(48) is not { } scour || Number(49) is not { } low || Number(50) is not { } high || Number(51) is not { } meticulous || Flag(54) is not { } scrutiny || Flag(56) is not { } intuition
            || scour==0 || low>high || high>maximum*4 || Number(26) is not { } scourAction || Number(29) is not { } brazenAction || Number(32) is not { } meticulousAction
            || Number(35) is not { } collectAction || Number(38) is not { } scrutinyAction || Number(41) is not { } focusAction)return null;
        // 0.480: « 8 % » beside Meticulous: its chance of costing no attempt. Unread: 0, so the plan does not count on it.
        int Percent(int index)
        {
            var label=$"{index}:text ";
            if(raw.Count<=index || !raw[index].StartsWith(label,StringComparison.Ordinal))return 0;
            return int.TryParse(new string(raw[index][label.Length..].Where(char.IsAsciiDigit).ToArray()),NumberStyles.None,CultureInfo.InvariantCulture,out var percent) && percent<=100 ? percent:0;
        }
        return new(item,(int)current,(int)maximum,(int)scour,(int)low,(int)high,(int)meticulous,scrutiny,intuition,scourAction,brazenAction,meticulousAction,collectAction,scrutinyAction,focusAction,Percent(52),Percent(58),
            // 0.488 (user request): the flag that follows each action's identifier: what the window offers this character. Absent: not held against it.
            Flag(28) ?? true,Flag(31) ?? true,Flag(34) ?? true,Flag(40) ?? true,Flag(43) ?? true,
            // 0.527: Priming Touch (value 44, its flag at 46, in the logs of 2026-11-04: false for the miner at level 85, true for the botanist at level 100).
            Number(44) ?? 0,Flag(46)==true);
    }
}

/// <summary>0.480: what the plan can count on beside the gains. Bonuses are measured in the game (0: not yet). Intuition comes by chance: it counts in the average, never in what is certain.</summary>
/// <param name="ScrutinyBonus">What Scrutiny adds to the next action.</param>
/// <param name="IntuitionBonus">What Intuition adds when it happens.</param>
/// <param name="Gp">GP left.</param>
/// <param name="ScrutinyCost">The action's GP cost, from the game's sheet; 0: unknown, not planned.</param>
/// <param name="FocusCost">Likewise for Collector's Focus.</param>
/// <param name="ScrutinyActive">Scrutiny is on the player now; the gains shown already hold it.</param>
/// <param name="UseScrutiny">The setting: the plan may spend GP on Scrutiny.</param>
/// <param name="RestoreCost">GP for one more attempt (Solid Reason, Ageless Words: « restores 1 integrity », docs/evidence/0.410-gp-actions.md); 0: not planned.</param>
/// <param name="MaxAttempts">The window's maximum: an attempt can only be restored below it.</param>
/// <param name="RiskTolerance">0.484: 100 chooses on the average alone, 0 on the worst outcome alone; between, a mix of both at each roll.</param>
/// <param name="IntuitionBase">Intuition's usual chance: the lowest shown for this item. A much higher one this turn means Collector's Focus is on. 0: this turn's.</param>
/// <param name="EurekaChance">0.520 (user report): what a restored attempt adds on average: from level 90, 50 % of Eureka Moment, hence of a free attempt (docs/evidence/0.410-gp-actions.md). Never in the worst case.</param>
/// <param name="EurekaReady">Eureka Moment is on the player now: one free attempt, for sure.</param>
/// <param name="PrimingCost">0.527 (user report): GP of Priming Touch, which doubles the chance that the next Meticulous costs no attempt (its description); 0: not planned.</param>
/// <param name="MeticulousBase">Meticulous's usual chance: the lowest shown for this item. About twice that this turn means Priming Touch is on. 0: this turn's.</param>
public sealed record CollectableBonuses(int ScrutinyBonus=0,int IntuitionBonus=0,int Gp=0,int ScrutinyCost=0,int FocusCost=0,bool ScrutinyActive=false,bool UseScrutiny=true,int RestoreCost=0,int MaxAttempts=0,int IntuitionBase=0,int RiskTolerance=100,decimal EurekaChance=0,bool EurekaReady=false,int PrimingCost=0,int MeticulousBase=0)
{
    public static CollectableBonuses None{get;}=new();
}

/// <summary>0.529 (user request): the risk is three steps with a meaning, not a percentage: safe, uncertain, try your luck.</summary>
public static class CollectableRisk
{
    public static readonly int[] Caps=[0,50,100];
    public const int Safe=0;
    /// <summary>The nearest step: a value saved by an older version lands on one.</summary>
    public static int Snap(int value)=>value<25 ? 0:value<75 ? 50:100;
}

public enum CollectableMove { Scour,Brazen,Meticulous,Collect,Scrutiny,Restore,Focus,Eureka,Priming }
/// <param name="Move">The best move now, on average.</param>
/// <param name="Target">The tier reached without counting on luck; Below if none.</param>
/// <param name="Selections">Actions of that sure plan.</param>
/// <param name="Collections">Times to collect after them, restored attempts included.</param>
/// <param name="Scrips">The scrips of the sure plan; 0 for an item reward.</param>
/// <param name="Impossible">Even the best rolls earn nothing with the attempts left.</param>
/// <param name="Scrutinies">Scrutiny uses of the sure plan.</param>
/// <param name="Expected">What the chosen moves earn on average: scrips, or tiers for an item reward.</param>
/// <param name="Worst">What they earn if every roll goes badly.</param>
/// <param name="Restores">Attempts restored in the sure plan.</param>
/// <param name="HighScrips">The reference: the most scrips of the high tier on this node for sure; null if not reachable for sure.</param>
/// <param name="HighMissing">What the sure plan lacks to reach the high threshold, all GP spent.</param>
/// <param name="GainLow">0.482: what the move really adds now, without the bonus of a ticked box unless it is on the player.</param>
/// <param name="GainHigh">The top of that gain, for Brazen.</param>
/// <param name="Path">0.533: the moves of the chosen step, if no roll brings anything extra.</param>
public sealed record CollectableAdvice(CollectableMove Move,CollectableTier Target,int Selections,int Collections,int Scrips,bool Impossible=false,int Scrutinies=0,decimal Expected=0,
    int Restores=0,int? HighScrips=null,int HighMissing=0,int GainLow=0,int GainHigh=0,decimal Worst=0,CollectablePath? Path=null);

/// <summary>0.533 (user request): the moves of a plan, and the GP they spend. Compared by content, as the advice is.</summary>
public sealed class CollectablePath(IReadOnlyList<(CollectableMove Move,int Count)> steps,int gp) : IEquatable<CollectablePath>
{
    public IReadOnlyList<(CollectableMove Move,int Count)> Steps {get;}=steps;
    public int Gp {get;}=gp;
    public bool Equals(CollectablePath? other)=>other is not null && Gp==other.Gp && Steps.SequenceEqual(other.Steps);
    public override bool Equals(object? other)=>Equals(other as CollectablePath);
    public override int GetHashCode()=>HashCode.Combine(Gp,Steps.Count);
}

/// <summary>
/// 0.479: the plan for the attempts left, for the most scrips on this node: each attempt spent raising collectability is one less to collect, so what counts is the tier reached times the items collected.
/// 0.480 (user reports: bonus actions not counted, Meticulous chosen at 8 %, high tier and most gathers as the reference): every move is compared on what it earns on average, luck included: Brazen's range
/// (taken as even across its bounds, the game gives no finer rule), Meticulous's chance of costing no attempt as the game shows it. GP goes where it earns most: Scrutiny once measured, Collector's Focus, or an attempt restored.
/// 0.481 (user report): Intuition counts at the chance the game shows, and Collector's Focus (x 1.75, its description) when it earns more. 0.520: Eureka counts in the average of a restored attempt, never in the worst case. A ticked estimate box is taken out of the gains, since it shows a bonus that may not happen. This turn's gains are those the window gives; later turns take the plain gain.
/// 0.484 (user request): the risk accepted is the player's: each roll counts as a mix of its average and its worst outcome, set by the tolerance. Nothing is executed.
/// </summary>
public static class CollectableAdvisor
{
    private const decimal Margin=0.01m;
    private const int BrazenSteps=5,LaterBrazenSteps=3,MaxStates=40000;
    private sealed record Gains(int Scour,int BrazenMin,int BrazenMax,int Meticulous,int Later);
    private readonly record struct Outcome(decimal Score,decimal Mean,decimal Worst,CollectableMove Move);

    /// <summary>This turn's gains without what a box or a Scrutiny on adds; the Scrutiny's own bonus is added by the search.</summary>
    private static Gains Sure(CollectableTurn turn,CollectableBonuses bonuses)
    {
        var added=turn.EstimateScrutiny || turn.EstimateIntuition || bonuses.ScrutinyActive;
        if(!added || turn.Extra is not { } extra)return new(turn.Scour,turn.BrazenMin,turn.BrazenMax,turn.Meticulous,added ? turn.BrazenMax-turn.BrazenMin:turn.Scour);
        return new(turn.Scour-extra,turn.BrazenMin-extra,turn.BrazenMax-extra,turn.Meticulous-extra,turn.Scour-extra);
    }

    public static CollectableAdvice? Advise(CollectableTurn turn,int attempts,CollectableRule? rule,CollectableBonuses? given=null)
    {
        ArgumentNullException.ThrowIfNull(turn);
        if(attempts<=0 || rule is null)return null;
        var bonuses=given ?? CollectableBonuses.None;
        // 0.520: a free attempt is there for sure; the very last gather closes the node, so it goes first.
        var lastBeforeEureka=bonuses.EurekaReady && attempts==1 && bonuses.MaxAttempts>1;
        if(bonuses.EurekaReady)attempts++;
        var eureka=Math.Clamp(bonuses.EurekaChance,0,1);
        var gains=Sure(turn,bonuses);
        var bonus=bonuses.UseScrutiny || bonuses.ScrutinyActive ? Math.Max(0,bonuses.ScrutinyBonus):0;
        var scrutinyCost=bonuses.UseScrutiny && bonus>0 && turn.ScrutinyOpen ? bonuses.ScrutinyCost:0;
        var fullScrutinyCost=scrutinyCost;
        var restoreCost=Math.Max(0,bonuses.RestoreCost);var gpNow=Math.Max(0,bonuses.Gp);
        var shownMeticulous=Math.Clamp(turn.MeticulousChance,0,100)/100m;
        var chance=bonuses.MeticulousBase>0 ? Math.Min(shownMeticulous,Math.Clamp(bonuses.MeticulousBase,0,100)/100m):shownMeticulous;
        // The chance shown is twice the usual one: Priming Touch is already on. Any other higher chance is the game's own (capture of 2026-11-05: 64 % after an Intuition, « Keen Eye »): it counts as shown, this turn.
        var primingOn=chance>0 && Math.Abs(shownMeticulous-chance*2)<=0.02m;
        var primingCost=turn.PrimingOpen && turn.MeticulousOpen && chance>0 && !primingOn ? Math.Max(0,bonuses.PrimingCost):0;
        var fullPrimingCost=primingCost;
        // 0.527: Intuition gives half the plain gain: 88 for 177 (log, 0.481), 100 for 200 (capture of 2026-11-05: 250 + 150 + 100 = 500). The measure of another item would be wrong here; it only says that Intuition is known.
        var intuitionGain=bonuses.IntuitionBonus>0 ? gains.Later/2:0;var focusCost=turn.FocusOpen ? Math.Max(0,bonuses.FocusCost):0;
        var fullFocusCost=focusCost;
        var shownChance=Math.Clamp(turn.IntuitionChance,0,100)/100m;
        var usualChance=bonuses.IntuitionBase>0 ? Math.Clamp(bonuses.IntuitionBase,0,100)/100m:shownChance;
        // The chance shown is much above the usual one: Collector's Focus is already on for this turn's action.
        var focusOn=shownChance>usualChance*1.4m;
        // 0.531 (user capture: Safe 65.9 on average, Balanced 70.2, both 40 at worst): a little of the average always counts, so that between two moves with the same floor the better one is taken.
        var caution=Math.Min(0.99m,1m-Math.Clamp(bonuses.RiskTolerance,0,100)/100m);
        var memo=new Dictionary<(int Value,int Attempts,int Gp,bool Active,bool Focus,bool First,bool Priming),Outcome>();
        // Collecting: the attempts left, and those the GP left can restore on the way, each with its chance of a free one on average.
        Outcome Collecting(int value,int left,int gp)
        {
            var worth=Worth(rule,Collectables.Tier(rule,value));var restored=restoreCost>0 && left>0 && bonuses.MaxAttempts>0 ? gp/restoreCost:0;
            var sure=worth*(Math.Max(0,left)+restored);var mean=sure+worth*restored*eureka;
            return new((1-caution)*mean+caution*sure,mean,sure,CollectableMove.Collect);
        }
        // A roll: its score mixes the average and the worst branch; average and worst of what is chosen are kept apart, to be shown.
        Outcome Roll(CollectableMove move,params (decimal Weight,Outcome Branch)[] branches)
        {
            var kept=branches.Where(row=>row.Weight>0).ToArray();
            var mean=kept.Sum(row=>row.Weight*row.Branch.Score);
            return new((1-caution)*mean+caution*kept.Min(row=>row.Branch.Score),kept.Sum(row=>row.Weight*row.Branch.Mean),kept.Min(row=>row.Branch.Worst),move);
        }

        Outcome Best(int value,int left,int gp,bool active,bool focus,bool first,bool priming)
        {
            value=Math.Min(value,turn.Maximum);
            // Nothing more to gain past the high threshold or the maximum. This also ends the search: a free Meticulous at the maximum would otherwise come back to the same state for ever, and a stack
            // overflow closes the game without a message (in-game report, 0.480). The memo and the bound on states keep it finite in every case.
            if(value>=turn.Maximum || value>=rule.High || left<=0 || memo.Count>MaxStates)return Collecting(value,left,gp);
            var key=(value,left,gp,active,focus,first,priming);
            if(memo.TryGetValue(key,out var known))return known;
            var plus=active ? bonus:0;
            var scour=(first ? gains.Scour:gains.Later)+plus;
            var low=(first ? gains.BrazenMin:gains.Later/2)+plus;
            var high=(first ? gains.BrazenMax:gains.Later*3/2)+plus;
            var meticulous=(first ? gains.Meticulous:gains.Later*3/4)+plus;
            var luck=intuitionGain<=0 ? 0m:focus ? Math.Min(1m,usualChance*1.75m):first ? shownChance:usualChance;
            // After any Selection, at this action's chance of Intuition; Scrutiny and Focus are then used up.
            // Priming Touch waits for the next Meticulous, its description says: other Selections leave it on.
            Outcome After(int gained,int attemptsLeft,int gpLeft,bool primedAfter)
                =>Roll(CollectableMove.Scour,(1-luck,Best(value+gained,attemptsLeft,gpLeft,false,false,false,primedAfter)),(luck,Best(value+gained+intuitionGain,attemptsLeft,gpLeft,false,false,false,primedAfter)));
            // In this order: on an equal score, collect now rather than spend; a sure gain rather than a roll; no GP rather than GP.
            var best=Collecting(value,left,gp);
            void Consider(Outcome outcome,CollectableMove move){if(outcome.Score>best.Score+Margin)best=outcome with {Move=move};}
            if(left>=2 && scour>0 && turn.ScourOpen)Consider(After(scour,left-1,gp,priming),CollectableMove.Scour);
            if(meticulous>0 && turn.MeticulousOpen)
            {
                // It costs no attempt at this chance; otherwise it costs one, and with the last one nothing is left to collect.
                var paid=left>=2 ? After(meticulous,left-1,gp,false):new Outcome(0,0,0,CollectableMove.Collect);
                // This turn: the chance the game shows, plus the usual chance if Priming Touch is bought now. Later turns: the usual one, doubled by Priming Touch.
                var free=first ? Math.Min(1m,shownMeticulous+(priming && !primingOn ? chance:0)):Math.Min(1m,priming ? chance*2:chance);
                Consider(free>0 ? Roll(CollectableMove.Meticulous,(free,After(meticulous,left,gp,false)),(1-free,paid)):paid,CollectableMove.Meticulous);
            }
            if(left>=2 && high>0 && turn.BrazenOpen)
            {
                var steps=first ? BrazenSteps:LaterBrazenSteps;
                var rolls=new (decimal Weight,Outcome Branch)[steps];
                for(var step=0;step<steps;step++)rolls[step]=(1m/steps,After(low+(high-low)*(2*step+1)/(2*steps),left-1,gp,priming));
                Consider(Roll(CollectableMove.Brazen,rolls),CollectableMove.Brazen);
            }
            if(left>=2 && !active && scrutinyCost>0 && gp>=scrutinyCost)Consider(Best(value,left,gp-scrutinyCost,true,focus,first,priming),CollectableMove.Scrutiny);
            if(left>=2 && !focus && !(first && focusOn) && focusCost>0 && gp>=focusCost && intuitionGain>0 && usualChance>0)Consider(Best(value,left,gp-focusCost,active,true,first,priming),CollectableMove.Focus);
            if(!priming && primingCost>0 && gp>=primingCost && meticulous>0)Consider(Best(value,left,gp-primingCost,active,focus,first,true),CollectableMove.Priming);
            // 0.488 (user request): only with one attempt left, so nothing is spent before it is needed.
            if(restoreCost>0 && gp>=restoreCost && left==1 && left<bonuses.MaxAttempts)Consider(eureka>0 ? Roll(CollectableMove.Restore,(eureka,Best(value,left+2,gp-restoreCost,active,focus,first,priming)),(1-eureka,Best(value,left+1,gp-restoreCost,active,focus,first,priming))):Best(value,left+1,gp-restoreCost,active,focus,first,priming),CollectableMove.Restore);
            memo[key]=best;
            return best;
        }

        var result=Best(turn.Current,attempts,gpNow,bonuses.ScrutinyActive,false,true,primingOn);
        if(result.Score<=0)
        {
            // A careful player may earn nothing for sure, yet luck could: only say impossible if even the bold search finds nothing.
            var kept=caution;caution=0;memo.Clear();
            var bold=Best(turn.Current,attempts,gpNow,bonuses.ScrutinyActive,false,true,primingOn);
            caution=kept;memo.Clear();
            if(bold.Mean<=0)return new(CollectableMove.Collect,CollectableTier.Below,0,attempts,0,true);
            result=Best(turn.Current,attempts,gpNow,bonuses.ScrutinyActive,false,true,primingOn);
        }
        // 0.531 (user capture): a step looked at the floor at each roll, so a bolder plan could reach the same final floor with a better average (67.3 against 70.2, both 40). A step keeps its floor
        // and takes the boldest search that still holds it. 0.532 (user capture: Safe 43.9, Balanced 40.2, both 20): every step does, or Balanced could earn less than Safe for the same floor.
        if(bonuses.RiskTolerance<100)
        {
            var floor=result.Worst;var safest=caution;
            foreach(var step in new[]{0m,0.25m,0.5m,0.75m}.Where(step=>step<safest))
            {
                caution=step;memo.Clear();
                var bolder=Best(turn.Current,attempts,gpNow,bonuses.ScrutinyActive,false,true,primingOn);
                if(bolder.Worst>=floor){result=bolder;safest=step;break;}
            }
            caution=safest;memo.Clear();
        }
        // 0.482 (user report: Scrutiny not advised): the order of the actions does not change the score, so « no GP now » won the tie and the Scrutiny the plan needs came later. If a bonus action pays at
        // all and costs nothing to do first, it is the move now: it is the plain order to play, and it counts on the very next action.
        if(result.Move is CollectableMove.Scour or CollectableMove.Brazen or CollectableMove.Meticulous)
        {
            decimal Without(bool scrutiny,bool focus)
            {
                scrutinyCost=scrutiny ? 0:fullScrutinyCost;focusCost=focus ? 0:fullFocusCost;memo.Clear();
                var worth=Best(turn.Current,attempts,gpNow,bonuses.ScrutinyActive,false,true,primingOn).Score;
                scrutinyCost=fullScrutinyCost;focusCost=fullFocusCost;memo.Clear();
                return worth;
            }
            if(!bonuses.ScrutinyActive && fullScrutinyCost>0 && gpNow>=fullScrutinyCost && attempts>=2 && Without(true,false)<result.Score-Margin
                && Best(turn.Current,attempts,gpNow-fullScrutinyCost,true,false,true,primingOn) is var scrutinyFirst && scrutinyFirst.Score>=result.Score-Margin)
                result=scrutinyFirst with {Move=CollectableMove.Scrutiny};
            else if(!focusOn && fullFocusCost>0 && gpNow>=fullFocusCost && attempts>=2 && intuitionGain>0 && usualChance>0 && Without(false,true)<result.Score-Margin
                && Best(turn.Current,attempts,gpNow-fullFocusCost,bonuses.ScrutinyActive,true,true,primingOn) is var focusFirst && focusFirst.Score>=result.Score-Margin)
                result=focusFirst with {Move=CollectableMove.Focus};
            // 0.527: Priming Touch, just before the Meticulous it doubles, when that is the move and it pays.
            else if(result.Move==CollectableMove.Meticulous && fullPrimingCost>0 && gpNow>=fullPrimingCost)
            {
                primingCost=0;memo.Clear();
                var without=Best(turn.Current,attempts,gpNow,bonuses.ScrutinyActive,false,true,false).Score;
                primingCost=fullPrimingCost;memo.Clear();
                var primed=Best(turn.Current,attempts,gpNow-fullPrimingCost,bonuses.ScrutinyActive,false,true,true);
                if(primed.Score>without+Margin)result=primed with {Move=CollectableMove.Priming};
            }
        }

        // The last gather ends the node: GP that can pay for one more goes first.
        if(result.Move==CollectableMove.Collect && attempts==1 && restoreCost>0 && gpNow>=restoreCost && bonuses.MaxAttempts>1
            && Worth(rule,Collectables.Tier(rule,Math.Min(turn.Current,turn.Maximum)))>0)result=result with {Move=CollectableMove.Restore};

        if(lastBeforeEureka)result=result with {Move=CollectableMove.Eureka};

        // The plan without luck, to say what is certain, and the reference: the high tier with the most gathers.
        int Reach(int selections,int scrutinies)=>selections==0 ? turn.Current:Math.Min(turn.Maximum,turn.Current+gains.Scour+(selections-1)*gains.Later+(scrutinies+(bonuses.ScrutinyActive ? 1:0))*bonus);
        var sure=0;var sureScrutinies=0;var sureRestores=0;var sureCollects=attempts;decimal sureValue=-1;
        int? highCollects=null;var furthest=turn.Current;
        var mostRestores=restoreCost>0 && bonuses.MaxAttempts>0 ? gpNow/restoreCost:0;
        for(var selections=0;selections<attempts+mostRestores;selections++)
        {
            var open=Math.Max(0,selections-(bonuses.ScrutinyActive ? 1:0));
            for(var scrutinies=0;scrutinies<=open && scrutinies*scrutinyCost<=gpNow && (scrutinies==0 || scrutinyCost>0);scrutinies++)
            {
                var restores=restoreCost>0 && bonuses.MaxAttempts>0 ? (gpNow-scrutinies*scrutinyCost)/restoreCost:0;
                var collects=attempts+restores-selections;
                if(collects<=0)continue;
                var reach=Reach(selections,scrutinies);
                furthest=Math.Max(furthest,reach);
                var tier=Collectables.Tier(rule,reach);
                if(tier==CollectableTier.High)highCollects=Math.Max(highCollects ?? 0,collects);
                var value=Worth(rule,tier)*collects;
                if(value>sureValue){sure=selections;sureScrutinies=scrutinies;sureRestores=restores;sureCollects=collects;sureValue=value;}
            }
        }
        if(sureValue<=0){sure=0;sureScrutinies=0;sureRestores=0;sureCollects=attempts;}
        var reached=sureValue>0 ? Collectables.Tier(rule,Reach(sure,sureScrutinies)):CollectableTier.Below;
        var scrips=rule.Scrips ? Reward(rule,reached)*sureCollects:0;
        // 0.533 (user request): the plan's moves, on the line where no roll brings anything extra (a free Meticulous, an Intuition; Brazen at the middle of its range). The panel asks again at each turn, so luck changes the rest.
        CollectablePath Walk()
        {
            memo.Clear();
            var steps=new List<(CollectableMove Move,int Count)>();var spent=0;
            void Add(CollectableMove move,int count=1)
            {
                if(steps.Count>0 && steps[^1].Move==move)steps[^1]=(move,steps[^1].Count+count);
                else steps.Add((move,count));
            }
            var value=Math.Min(turn.Current,turn.Maximum);var left=attempts;var gp=gpNow;var active=bonuses.ScrutinyActive;var focus=false;var first=true;var priming=primingOn;
            var move=result.Move;
            for(var guard=0;guard<32;guard++)
            {
                var plus=active ? bonus:0;
                switch(move)
                {
                    case CollectableMove.Scrutiny:Add(move);gp-=fullScrutinyCost;spent+=fullScrutinyCost;active=true;break;
                    case CollectableMove.Focus:Add(move);gp-=fullFocusCost;spent+=fullFocusCost;focus=true;break;
                    case CollectableMove.Priming:Add(move);gp-=fullPrimingCost;spent+=fullPrimingCost;priming=true;break;
                    case CollectableMove.Restore:Add(move);gp-=restoreCost;spent+=restoreCost;left++;break;
                    case CollectableMove.Eureka:Add(move);break;
                    case CollectableMove.Scour:Add(move);value+=(first ? gains.Scour:gains.Later)+plus;left--;active=false;focus=false;first=false;break;
                    case CollectableMove.Brazen:Add(move);value+=(first ? (gains.BrazenMin+gains.BrazenMax)/2:gains.Later)+plus;left--;active=false;focus=false;first=false;break;
                    case CollectableMove.Meticulous:Add(move);value+=(first ? gains.Meticulous:gains.Later*3/4)+plus;left--;active=false;focus=false;first=false;priming=false;break;
                    default:
                        // The last moves: what is left is gathered, each restored attempt after the one it follows.
                        var restores=restoreCost>0 && left>0 && bonuses.MaxAttempts>0 && Worth(rule,Collectables.Tier(rule,value))>0 ? gp/restoreCost:0;
                        if(left>0)Add(CollectableMove.Collect,left);
                        for(var restored=0;restored<restores;restored++){Add(CollectableMove.Restore);spent+=restoreCost;Add(CollectableMove.Collect);}
                        return new(steps.AsReadOnly(),spent);
                }
                if(left<=0)return new(steps.AsReadOnly(),spent);
                move=Best(value,left,gp,active,focus,first,priming).Move;
            }
            return new(steps.AsReadOnly(),spent);
        }
        var path=Walk();
        var onNow=bonuses.ScrutinyActive ? bonus:0;
        var (gainLow,gainHigh)=result.Move switch
        {
            CollectableMove.Scour=>(gains.Scour+onNow,gains.Scour+onNow),
            CollectableMove.Brazen=>(gains.BrazenMin+onNow,gains.BrazenMax+onNow),
            CollectableMove.Meticulous=>(gains.Meticulous+onNow,gains.Meticulous+onNow),
            _=>(0,0),
        };
        return new(result.Move,reached,sure,sureCollects,scrips,false,sureScrutinies,result.Mean,sureRestores,
            highCollects is { } most && rule.Scrips ? rule.RewardHigh*most:null,Math.Max(0,rule.High-furthest),gainLow,gainHigh,result.Worst,path);
    }
    private static decimal Worth(CollectableRule rule,CollectableTier tier)
        =>rule.Scrips && rule.RewardHigh>0 ? Reward(rule,tier):tier switch{CollectableTier.High=>3,CollectableTier.Mid=>2,CollectableTier.Low=>1,_=>0};
    private static int Reward(CollectableRule rule,CollectableTier tier)
        =>tier switch{CollectableTier.High=>rule.RewardHigh,CollectableTier.Mid=>rule.RewardMid,CollectableTier.Low=>rule.RewardLow,_=>0};
}
