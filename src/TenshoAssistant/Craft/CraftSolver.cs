namespace TenshoAssistant.Craft;

// 0.438 (user request): Forbidden = the key of the actions the player forbids (CraftActionRules), never used by the search.
public sealed record CraftSolveOptions(int TargetQuality,int MaxSteps=40,int BeamWidth=128,int MaxExpanded=100000,int ExtraWait=0,int? BuffExtraWait=null,string Forbidden="",bool Raphael=false,bool Fresh=false);
public sealed record CraftSolveResult(CraftSimulationResult? Simulation,int TargetQuality,bool TargetReached,int Expanded,int Depth,
    bool BeamPruned,bool NodeLimitReached,bool DepthLimitReached,int? WaitSeconds=null,bool FromCache=false);

/// <summary>The synthesis as it stands (A-04): what the game allows now, under the condition it shows.</summary>
public sealed record CraftSuggestInput(RecipeDefinition Recipe,CraftStats Stats,CraftSimState State,CraftStepCondition Condition,int GoodPercent,IReadOnlyList<CraftActionKind> Available);
public sealed record CraftSuggestion(CraftActionKind Move,int FinalQuality,bool TargetReached,IReadOnlyList<CraftActionKind> Then);

/// <summary>Deterministic bounded beam search. Failure to find a route does not prove impossibility.</summary>
public static class CraftSolver
{
    internal static bool BetterCompletion(CraftSimState candidate,int wait,CraftSimState? previous,int previousWait,int target)
    {
        if(previous is null)return true;
        var reached=candidate.Quality>=target;var wasReached=previous.Quality>=target;
        if(reached!=wasReached)return reached;
        if(!reached && candidate.Quality!=previous.Quality)return candidate.Quality>previous.Quality;
        if(wait!=previousWait)return wait<previousWait;
        if(candidate.Step!=previous.Step)return candidate.Step<previous.Step;
        if(candidate.Cp!=previous.Cp)return candidate.Cp>previous.Cp;
        return candidate.Quality>previous.Quality;
    }
    private sealed record Node(CraftSimState State,Node? Parent,CraftActionKind Action,int WaitSeconds=0);
    public static CraftSolveResult Solve(RecipeDefinition recipe,CraftStats stats,int startingQuality,CraftSolveOptions options,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var initial=CraftSimulation.Run(recipe,stats,startingQuality,[],cancellation); var p=recipe.Simulation!;
        if(options.TargetQuality<0 || options.TargetQuality>p.Quality || options.MaxSteps is <1 or >100 || options.BeamWidth is <1 or >512 || options.MaxExpanded is <1 or >500000 || options.ExtraWait is <0 or >3 || options.BuffExtraWait is <0 or >3)
            throw new ArgumentOutOfRangeException(nameof(options));
        var (best,expanded,searchedDepth,pruned,limited,depthLimited)=Search(recipe,stats,initial,initial.Final,options,cancellation);
        cancellation.ThrowIfCancellationRequested();
        CraftSimulationResult? simulation=null;
        if(best is not null)
        {
            var sequence=new List<CraftActionKind>();
            for(var node=best;node.Parent is not null;node=node.Parent) sequence.Add(node.Action);
            sequence.Reverse(); simulation=CraftSimulation.Run(recipe,stats,startingQuality,sequence,cancellation);
            if(simulation.Error is not null || simulation.Final!=best.State) throw new InvalidOperationException("Search replay mismatch.");
        }
        return new(simulation,options.TargetQuality,simulation?.Final.Quality>=options.TargetQuality,expanded,searchedDepth,pruned,limited,depthLimited,best?.WaitSeconds);
    }

    /// <summary>
    /// A-04 (DEC-028, 0.509): the move to suggest from the synthesis as it stands. Each move the game allows now is played under the real condition, then the rest is
    /// searched under the normal condition, which never counts on luck. It only proposes: nothing is played. Null when no move leads anywhere within the budget.
    /// </summary>
    public static CraftSuggestion? Suggest(CraftSuggestInput input,int target,string forbidden,CancellationToken cancellation=default,int beamWidth=32,int maxExpanded=3000)
    {
        var recipe=input.Recipe;var stats=input.Stats;var p=recipe.Simulation!;
        var initial=CraftSimulation.Run(recipe,stats,0,[],cancellation);
        var options=new CraftSolveOptions(Math.Clamp(target,0,p.Quality),BeamWidth:beamWidth,MaxExpanded:maxExpanded,Forbidden:forbidden);
        CraftSuggestion? best=null;CraftSimState? bestFinal=null;var bestWait=0;
        foreach(var kind in input.Available)
        {
            cancellation.ThrowIfCancellationRequested();
            if(CraftSimulation.Random(kind) || CraftActionRules.Forbidden(forbidden,kind))continue;
            var step=CraftSimulation.Apply(p,stats,recipe.Level,initial.BaseProgress,initial.BaseQuality,input.State,kind,out _,input.Condition,input.GoodPercent);
            if(step is null || step.After.Finish==CraftFinish.Broken)continue;
            CraftSimState final;var wait=0;var then=new List<CraftActionKind>();
            if(step.After.Finish==CraftFinish.Completed)final=step.After;
            else
            {
                var (found,_,_,_,_,_)=Search(recipe,stats,initial,step.After,options,cancellation);
                if(found is null)continue;
                final=found.State;wait=found.WaitSeconds;
                for(var node=found;node.Parent is not null;node=node.Parent)then.Add(node.Action);
                then.Reverse();
            }
            if(!BetterCompletion(final,wait,bestFinal,bestWait,options.TargetQuality))continue;
            bestFinal=final;bestWait=wait;
            best=new(kind,final.Quality,final.Quality>=options.TargetQuality,then.AsReadOnly());
        }
        return best;
    }

    private static (Node? Best,int Expanded,int Depth,bool Pruned,bool Limited,bool DepthLimited) Search(RecipeDefinition recipe,CraftStats stats,CraftSimulationResult initial,CraftSimState start,CraftSolveOptions options,CancellationToken cancellation)
    {
        var p=recipe.Simulation!;
        // A-13 (0.434): no action that succeeds by chance, nor Careful Observation or Final Appraisal (no use under the normal condition).
        var actions=CraftActionCatalog.Specs.Where(spec=>spec.Level<=stats.Level && (spec.Kind!=CraftActionKind.Manipulation || stats.ManipulationAllowed)
            && !CraftSimulation.Random(spec.Kind) && spec.Kind is not (CraftActionKind.CarefulObservation or CraftActionKind.FinalAppraisal)
            && !CraftActionRules.Forbidden(options.Forbidden,spec.Kind)).ToArray();
        var minimumWait=actions.Min(action=>CraftMacroExporter.Wait(action.Kind,options.ExtraWait,options.BuffExtraWait));
        var beam=new List<Node>{new(start,null,default)}; Node? best=null;
        var expanded=0; var depth=0; var searchedDepth=0; var pruned=false; var limited=false; var depthLimited=false;
        bool Reached(Node node)=>node.State.Quality>=options.TargetQuality;
        bool Better(Node candidate,Node? previous)=>BetterCompletion(candidate.State,candidate.WaitSeconds,previous?.State,previous?.WaitSeconds ?? 0,options.TargetQuality);
        for(depth=1;depth<=options.MaxSteps && beam.Count>0;depth++)
        {
            searchedDepth=depth;
            var candidates=new Dictionary<CraftSimState,Node>();
            var finishers=new Dictionary<CraftSimState,double>();
            foreach(var node in beam)
            {
                foreach(var action in actions)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if(expanded==options.MaxExpanded) {limited=true;break;}
                    expanded++;
                    var kind=action.Kind;
                    if(node.State.Quality>=options.TargetQuality && (kind is CraftActionKind.BasicTouch or CraftActionKind.StandardTouch or CraftActionKind.AdvancedTouch or CraftActionKind.Byregot or CraftActionKind.Reflect or CraftActionKind.Innovation or CraftActionKind.GreatStrides
                        or CraftActionKind.PreparatoryTouch or CraftActionKind.PrudentTouch or CraftActionKind.RefinedTouch or CraftActionKind.TrainedEye or CraftActionKind.TrainedFinesse)) continue;
                    if((kind is CraftActionKind.MastersMend or CraftActionKind.ImmaculateMend) && node.State.Durability==p.Durability) continue;
                    var step=CraftSimulation.Apply(p,stats,recipe.Level,initial.BaseProgress,initial.BaseQuality,node.State,kind,out _);
                    if(step is null || step.After.Finish==CraftFinish.Broken) continue;
                    var child=new Node(step.After,node,kind,node.WaitSeconds+CraftMacroExporter.Wait(kind,options.ExtraWait,options.BuffExtraWait));
                    if(step.After.Finish==CraftFinish.Completed) {if(Better(child,best)) best=child;}
                    else
                    {
                        if(!candidates.TryGetValue(step.After,out var previous) || child.WaitSeconds<previous.WaitSeconds)candidates[step.After]=child;
                        if(kind==CraftActionKind.Byregot && (step.After.Quality>=options.TargetQuality || node.State.GreatStrides>0 && node.State.InnerQuiet==10))
                            finishers[step.After]=4+(double)step.After.Quality/Math.Max(1,p.Quality);
                        if(kind==CraftActionKind.GreatStrides && stats.Level>=50 && step.After.InnerQuiet>0 && depth<options.MaxSteps && expanded<options.MaxExpanded)
                        {
                            // One bounded lookahead through the shared transition; it consumes search budget.
                            expanded++;
                            var byregot=CraftSimulation.Apply(p,stats,recipe.Level,initial.BaseProgress,initial.BaseQuality,step.After,CraftActionKind.Byregot,out _);
                            if(byregot is not null && byregot.After.Finish!=CraftFinish.Broken
                                && (step.After.InnerQuiet==10 || byregot.After.Quality>=options.TargetQuality))
                                finishers[step.After]=2+(double)byregot.After.Quality/Math.Max(1,p.Quality);
                        }
                    }
                }
                if(limited) break;
            }
            if(limited || best is not null && Reached(best) && (depth+1)*minimumWait>=best.WaitSeconds) break;
            // Keep different progress/quality emphases, rather than letting one scalar erase all finishing routes.
            var nodes=candidates.Values.ToArray();
            double Score(Node node,int emphasis)
            {
                var state=node.State; var q=options.TargetQuality==0 ? 1:Math.Min(1,(double)state.Quality/options.TargetQuality);
                var progress=(double)state.Progress/p.Progress;
                return q*(emphasis==0 ? 4:emphasis==1 ? 2:1)+progress*(emphasis==0 ? 1:emphasis==1 ? 2:4)
                    +.6*state.Durability/p.Durability+.6*state.Cp/Math.Max(1,stats.Cp)+.08*state.InnerQuiet
                    +.02*(state.Veneration+state.Innovation+state.GreatStrides+state.WasteNot+state.Manipulation+state.MuscleMemory);
            }
            if(nodes.Length>options.BeamWidth)
            {
                pruned=true; var selected=new HashSet<CraftSimState>(); var next=new List<Node>(); var share=Math.Max(1,options.BeamWidth/3);
                foreach(var node in nodes.Where(node=>finishers.ContainsKey(node.State))
                    .OrderByDescending(node=>finishers[node.State]).ThenBy(node=>node.WaitSeconds).Take(options.BeamWidth/4))
                    if(selected.Add(node.State))next.Add(node);
                // Spending Inner Quiet on Byregot drops the stack score despite a real quality gain.
                // Reserve a bounded fraction for both stored-stack and already-converted-quality paths.
                var quietRepresentatives=nodes.GroupBy(node=>node.State.InnerQuiet).SelectMany(group=>new[]
                {
                    group.OrderByDescending(node=>node.State.Quality).ThenByDescending(node=>Score(node,1)).First(),
                    group.OrderByDescending(node=>Score(node,1)).First()
                }).DistinctBy(node=>node.State).OrderByDescending(node=>Score(node,1))
                    .Take(Math.Min(options.BeamWidth/4,22));
                foreach(var node in quietRepresentatives)
                    if(selected.Add(node.State))next.Add(node);
                for(var emphasis=0;emphasis<3;emphasis++)
                    foreach(var node in nodes.OrderByDescending(node=>Score(node,emphasis)).Take(share))
                        if(next.Count<options.BeamWidth && selected.Add(node.State)) next.Add(node);
                foreach(var node in nodes.OrderByDescending(node=>Score(node,1)))
                    if(next.Count<options.BeamWidth && selected.Add(node.State)) next.Add(node);
                beam=next;
            }
            else beam=nodes.ToList();
            if(depth==options.MaxSteps && beam.Count>0) depthLimited=true;
        }
        return (best,expanded,searchedDepth,pruned,limited,depthLimited);
    }
}
