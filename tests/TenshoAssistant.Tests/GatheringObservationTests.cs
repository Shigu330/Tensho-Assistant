using TenshoAssistant.Gathering;

internal static class GatheringObservationTests
{
    private static GatheringRawSlot[] Slots()=>Enumerable.Range(0,8).Select(index=>new GatheringRawSlot(index,(uint)(index+10),0,index<2," 75 % ","0%","3",(uint)(index+10),true)).ToArray();
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action){var rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,"invalid frame accepted");}
    private static void Publish(GatheringObservationService service,int ticket,ulong owner,GatheringRawSlot[]? slots=null)=>service.Publish(ticket,owner,16,100,600,900,1,"4","6",slots ?? Slots());
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Gathering observed numeric text is strict and missing is never zero or one",()=>
        {
            Check(GatheringObservedFrame.Number("0 %",0,100,true)==0 && GatheringObservedFrame.Number("100%",0,100,true)==100,"zero and cap");
            Check(GatheringObservedFrame.Number(" 4 ",0,12)==4,"integer");
            foreach(var text in new string?[]{null,"","-","—","12.5%","+1","1/4","60% extra","60%%","６０%","101%",new('1',65)})
                Check(GatheringObservedFrame.Number(text,0,100,true) is null,"ambiguous text refused");
            Check(GatheringObservedFrame.Number("",1,20) is null && GatheringObservedFrame.Number("0",1,20) is null,"quantity not default one");
        }),
        ("Gathering observation preserves UI order identity and only visible rows",()=>
        {
            var source=Slots();source[1]=source[1] with {Success="bad",Boon="-",Quantity="",Flags=123};
            var frame=new GatheringObservedFrame(16,100,0,900,1,"4","6",source);source[0]=source[0] with {ItemId=99,BaseItemId=99};
            Check(frame.Slots.Count==2 && frame.Slots[0].ItemId==10 && frame.Slots[1].Index==1 && frame.Slots[1].Flags==123,"row order and copied data");
            Check(frame.Slots[0].SuccessPercent==75 && frame.Slots[0].BoonPercent==0 && frame.Slots[0].DisplayedQuantity==3,"displayed values only");
            Check(frame.Slots[1].SuccessPercent is null && frame.Slots[1].BoonPercent is null && frame.Slots[1].DisplayedQuantity is null,"unknown fields independent");
            Check(frame.Gp==0 && frame.Attempts==4 && frame.TotalAttempts==6 && frame.Slots is not GatheringObservedSlot[],"zero GP valid and owned immutable collection");
        }),
        ("Gathering partial or contradictory integrity never creates usable attempts",()=>
        {
            foreach(var (left,total) in new (string?,string?)[]{("4",null),(null,"6"),("7","6"),("13","13"),("-","6"),("4","0")})
                Check(new GatheringObservedFrame(17,50,200,600,1,left,total,Slots()).Attempts is null,"integrity unknown");
            var exhausted=new GatheringObservedFrame(17,50,200,600,1,"0","6",Slots());
            Check(exhausted.Attempts==0,"zero attempts explicit, not missing");
        }),
        ("Gathering encoded and special IDs cannot enter the standard context unresolved",()=>
        {
            var slots=Slots();slots[0]=slots[0] with {ItemId=500010,BaseItemId=10};
            slots[1]=slots[1] with {ItemId=2000001,BaseItemId=0,ContextResolved=false};
            var frame=new GatheringObservedFrame(16,100,0,900,1,"4","6",slots);
            Check(frame.Slots[0].ItemId==10 && frame.Slots[0].RawItemId==500010,"normalized collectible identity");
            Check(frame.Slots[1].ItemId==0 && frame.Slots[1].RawItemId==2000001,"event ID never guessed as ordinary Item row");
        }),
        ("Gathering malformed actor phase and row sets are rejected",()=>
        {
            Reject(()=>_=new GatheringObservedFrame(18,100,0,900,1,"4","4",Slots()));
            Reject(()=>_=new GatheringObservedFrame(16,0,0,900,1,"4","4",Slots()));
            Reject(()=>_=new GatheringObservedFrame(16,100,901,900,1,"4","4",Slots()));
            Reject(()=>_=new GatheringObservedFrame(16,100,0,10001,1,"4","4",Slots()));
            Reject(()=>_=new GatheringObservedFrame(16,100,0,900,0,"4","4",Slots()));
            Reject(()=>_=new GatheringObservedFrame(16,100,0,900,1,"4","4",Slots()[..7]));
            var duplicate=Slots();duplicate[7]=duplicate[0];Reject(()=>_=new GatheringObservedFrame(16,100,0,900,1,"4","4",duplicate));
            duplicate=Slots();duplicate[7]=duplicate[7] with {Index=8};Reject(()=>_=new GatheringObservedFrame(16,100,0,900,1,"4","4",duplicate));
        }),
        ("Gathering resource transfer owns exact GP level attempts and excludes UI rates",()=>
        {
            var now=DateTimeOffset.UtcNow;var frame=new GatheringObservedFrame(16,100,0,900,1,"4","6",Slots());
            var state=new GatheringObservationState(true,42,GatheringObservationStatus.Observed,now,frame,null);
            var copy=state.CopyResources(now)!;
            Check(copy==new GatheringResourceCopy(42,16,100,0,4,now),"only established resources copied");
            foreach(var other in new[]{state with {Enabled=false},state with {Owner=0},state with {ObservedAt=null},state with {Frame=null},state with {Status=GatheringObservationStatus.Waiting}})
                Check(other.CopyResources(now) is null,"incomplete or disabled");
            Check(state.CopyResources(now.AddSeconds(2)) is not null && state.CopyResources(now.AddSeconds(2).AddTicks(1)) is null && state.CopyResources(now.AddTicks(-1)) is null,"age boundary");
            Check((state with {Frame=new(16,100,0,900,2,"4","6",Slots())}).CopyResources(now) is null,"quick gathering not transferable");
            Check((state with {Frame=new(16,100,0,900,1,"0","6",Slots())}).CopyResources(now) is null,"exhausted not transferable");
        }),
        ("Gathering observation is opt-in and owner changes revoke prior reads",()=>
        {
            using var service=new GatheringObservationService();Check(service.BeginRead(42) is null,"off by default");service.SetEnabled(true);
            var first=service.BeginRead(42)!.Value;Publish(service,first,42);Check(service.State.Frame is not null,"published");
            var next=service.BeginRead(43)!.Value;Publish(service,first,42);Check(service.State.Frame is null,"foreign old lease");Publish(service,next,43);
            service.BeginRead(0);Publish(service,next,43);Check(service.State.Frame is null && service.State.Status==GatheringObservationStatus.Offline,"logout");
            service.BeginRead(43);Publish(service,next,43);Check(service.State.Frame is null,"same owner new lifetime");
        }),
        ("Gathering observation hidden absent error and bad data erase old observations",()=>
        {
            using var service=new GatheringObservationService();service.SetEnabled(true);
            foreach(var reason in new[]{GatheringObservationStatus.AddonAbsent,GatheringObservationStatus.NotReady})
            {
                var ticket=service.BeginRead(42)!.Value;Publish(service,ticket,42);service.Missing(ticket,42,reason);Publish(service,ticket,42);
                Check(service.State.Frame is null && service.State.Status==reason,"missing revokes");
            }
            var read=service.BeginRead(42)!.Value;Publish(service,read,42);service.Fail(read,42,new Exception("fixture"));Publish(service,read,42);
            Check(service.State.Frame is null && service.State.Error=="fixture","error revokes");
            read=service.BeginRead(42)!.Value;Publish(service,read,42,[]);Publish(service,read,42);
            Check(service.State.Frame is null && service.State.Status==GatheringObservationStatus.Error,"invalid frame revokes");
        }),
        ("Gathering observation toggles and disposal reject outstanding reads",()=>
        {
            var service=new GatheringObservationService();service.SetEnabled(true);var first=service.BeginRead(42)!.Value;
            service.SetEnabled(false);service.SetEnabled(true);service.BeginRead(42);Publish(service,first,42);Check(service.State.Frame is null,"toggle");
            var read=service.BeginRead(42)!.Value;service.Dispose();Publish(service,read,42);service.SetEnabled(true);service.Dispose();
            Check(service.BeginRead(42) is null && service.State.Status==GatheringObservationStatus.Disabled,"final disposal");
        })
    };
}
