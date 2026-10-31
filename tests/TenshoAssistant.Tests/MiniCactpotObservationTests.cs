using TenshoAssistant.MiniGames;

internal static class MiniCactpotObservationTests
{
    private static readonly int[] Board=[1,0,3,0,5,0,0,0,9];
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Cactpot observation copies row-major zeros without exposing caller mutation",()=>
        {
            var source=Board.ToArray();var frame=new MiniCactpotObservedFrame(source);source[0]=8;
            Check(frame.Board.SequenceEqual(Board) && frame.Revealed==4 && frame.Playable && frame.Board is not int[],"owned row-major board");
            foreach(var invalid in new[]{new int[8],new[]{1,1,0,0,0,0,0,0,0},new[]{-1,0,0,0,0,0,0,0,0},new[]{10,0,0,0,0,0,0,0,0}})
            {var rejected=false;try{_=new MiniCactpotObservedFrame(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid shape value or duplicate");}
            Check(!new MiniCactpotObservedFrame(new int[9]).Playable && !new MiniCactpotObservedFrame(Enumerable.Range(1,9).ToArray()).Playable,"initial and finished tickets never solved");
        }),
        ("Cactpot overlay keeps button rectangles only when complete and maps solver lines to game buttons",()=>
        {
            var tiles=Enumerable.Range(0,9).Select(index=>new MiniCactpotRect(index*10,0,8,8)).ToArray();
            var lanes=Enumerable.Range(0,8).Select(index=>new MiniCactpotRect(index*10,50,8,8)).ToArray();
            var frame=new MiniCactpotObservedFrame(Board,tiles,lanes);tiles[0]=new(99,99,1,1);
            Check(frame.Tiles is { Count:9 } && frame.Tiles[0].X==0 && frame.Lanes is { Count:8 },"owned copies of nine cells and eight lines");
            Check(new MiniCactpotObservedFrame(Board,tiles[..8],lanes).Tiles is null,"missing cell: no cell overlay");
            Check(new MiniCactpotObservedFrame(Board,tiles,lanes.Select((rect,index)=>index==3 ? rect with { Width=float.NaN }:rect).ToArray()).Lanes is null,"invalid line: no line overlay");
            // Solver lines: rows, columns, ↘, ↙. Game LaneSelector: ↘, columns 1–3, ↙, rows 1–3.
            Check(MiniCactpotObservedFrame.LaneOfLine.SequenceEqual(new[]{5,6,7,1,2,3,0,4}) && MiniCactpotObservedFrame.LaneOfLine.Distinct().Count()==8,"line to button mapping");
        }),
        ("Cactpot observation is opt-in and old owner leases cannot publish",()=>
        {
            using var service=new MiniCactpotObservationService();Check(service.BeginRead(42) is null,"default disabled");service.SetEnabled(true);
            var first=service.BeginRead(42)!.Value;service.Publish(first,42,Board);Check(service.State.Frame is not null,"published");
            var next=service.BeginRead(43)!.Value;Check(service.State.Frame is null,"owner cleared");service.Publish(first,42,Board);Check(service.State.Frame is null,"late owner rejected");
            service.Publish(next,43,Board);service.BeginRead(0);Check(service.State.Status==MiniCactpotObservationStatus.Offline && service.State.Frame is null,"logout");
            service.Publish(next,43,Board);Check(service.State.Frame is null,"old owner after logout");
        }),
        ("Cactpot disabled reenabled and disposed services reject outstanding leases",()=>
        {
            var service=new MiniCactpotObservationService();service.SetEnabled(true);var ticket=service.BeginRead(42)!.Value;
            service.SetEnabled(false);service.SetEnabled(true);service.BeginRead(42);service.Publish(ticket,42,Board);Check(service.State.Frame is null,"toggle revoked");
            var current=service.BeginRead(42)!.Value;service.Dispose();service.Publish(current,42,Board);service.SetEnabled(true);
            Check(service.State.Status==MiniCactpotObservationStatus.Disabled && service.BeginRead(42) is null,"dispose final");service.Dispose();
        }),
        ("Cactpot absent hidden error and malformed publication clear prior frames and revoke leases",()=>
        {
            using var service=new MiniCactpotObservationService();service.SetEnabled(true);
            foreach(var reason in new[]{MiniCactpotObservationStatus.AddonAbsent,MiniCactpotObservationStatus.NotReady})
            {
                var ticket=service.BeginRead(42)!.Value;service.Publish(ticket,42,Board);service.Missing(ticket,42,reason);service.Publish(ticket,42,Board);
                Check(service.State.Frame is null && service.State.Status==reason,"missing revokes");
            }
            var read=service.BeginRead(42)!.Value;service.Publish(read,42,Board);service.Fail(read,42,new Exception("fixture"));service.Publish(read,42,Board);
            Check(service.State.Error=="fixture" && service.State.Frame is null,"failure revokes");
            read=service.BeginRead(42)!.Value;service.Publish(read,42,new int[8]);service.Publish(read,42,Board);
            Check(service.State.Status==MiniCactpotObservationStatus.Error && service.State.Frame is null,"malformed clears and revokes");
        }),
        ("Cactpot solver gate requires owner enabled playable observation and bounded age",()=>
        {
            var now=DateTimeOffset.UtcNow;var state=new MiniCactpotObservationState(true,42,MiniCactpotObservationStatus.Observed,now,new(Board),null);
            Check(state.CanSolve(now) && state.CanSolve(now.AddSeconds(2)),"fresh boundary");
            Check(!state.CanSolve(now.AddTicks(-1)) && !state.CanSolve(now.AddSeconds(2).AddTicks(1)),"future and stale");
            foreach(var other in new[]{state with {Owner=0},state with {Enabled=false},state with {Status=MiniCactpotObservationStatus.Waiting},state with {Frame=null},state with {ObservedAt=null},state with {Frame=new(new int[9])}})
                Check(!other.CanSolve(now),"incomplete cannot solve");
        }),
        ("Cactpot session solves immutable input with owner and revision context",()=>
        {
            using var session=new MiniCactpotSolveSession();var source=Board.ToArray();var task=session.StartAsync(source,42,3);source[0]=8;task.GetAwaiter().GetResult();
            var expected=MiniCactpotSolver.Solve(Board);
            Check(session.State.Result?.ExpectedReward==expected.ExpectedReward && session.State.Owner==42 && session.State.Revision==3 && session.State.Board!.SequenceEqual(Board),"copied task input");
            session.ValidateContext(42,4);Check(session.State.Board is null && session.State.Result is null,"revision invalidates");
        }),
        ("Cactpot late worker success cannot replace a newer ticket",()=>
        {
            using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
            using var session=new MiniCactpotSolveSession((board,token)=>
            {
                if(board[0]==1){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException();}
                return new(true,[],board[0],0);
            });
            var old=session.StartAsync(Board,42,1);Check(entered.Wait(TimeSpan.FromSeconds(5)),"old started");
            var next=Board.ToArray();next[0]=2;session.StartAsync(next,43,2).GetAwaiter().GetResult();release.Set();old.GetAwaiter().GetResult();
            Check(session.State.Result?.ExpectedReward==2 && session.State.Owner==43,"old success rejected despite ignored cancellation");
        }),
        ("Cactpot late errors are rejected after owner cancellation and lifetime changes",()=>
        {
            foreach(var mode in new[]{0,1,2})
            {
                using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
                using var session=new MiniCactpotSolveSession((_,_)=>{entered.Set();if(!release.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException();throw new Exception("late");});
                var task=session.StartAsync(Board,42,1);Check(entered.Wait(TimeSpan.FromSeconds(5)),"started");
                if(mode==0)session.ValidateContext(43,1);else if(mode==1)session.Cancel();else session.Dispose();release.Set();task.GetAwaiter().GetResult();
                Check(session.State.Board is null && session.State.Result is null && session.State.Error is null && !session.State.Loading,"late error rejected");
            }
        }),
        ("Cactpot session preserves solver input errors instead of publishing a result",()=>
        {
            using var session=new MiniCactpotSolveSession();session.StartAsync(new int[9],0,1).GetAwaiter().GetResult();
            Check(session.State.Result is null && session.State.Error is not null && !session.State.Loading,"no revealed digit rejected");
        })
    };
}
