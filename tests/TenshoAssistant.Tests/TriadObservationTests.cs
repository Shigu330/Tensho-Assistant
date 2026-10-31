using TenshoAssistant.MiniGames;

internal static class TriadObservationTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T:Exception
    { try {action();} catch(T) {return;} throw new Exception($"Expected {typeof(T).Name}"); }
    private static readonly TriadRawCard Known = new(true,1,2,3,4,5,6,7);
    private static TriadRawCard[] Slots(int count) => Enumerable.Repeat(Known,count).ToArray();
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Triad observation distinguishes empty hidden known and incomplete without leaking stale faces",()=>
        {
            var empty=TriadObservedCard.Copy(Known with {HasCard=false});
            Check(empty.Presence==TriadObservedPresence.Empty && empty.Owner is null && empty.Stars==0 && empty.Top==0, "absent slot clears old bytes");
            var hidden=TriadObservedCard.Copy(Known with {Top=0,Right=0,Bottom=0,Left=0});
            Check(hidden.Presence==TriadObservedPresence.Hidden && hidden.Owner==TriadPlayer.Blue && hidden.Stars==0 && hidden.TypeId==0 && hidden.Right==0, "hidden metadata not exposed");
            var known=TriadObservedCard.Copy(Known);
            Check(known.Presence==TriadObservedPresence.Known && known.Top==4 && known.Right==5 && known.Bottom==6 && known.Left==7
                && known.Stars==2 && known.TypeId==3 && known.Owner==TriadPlayer.Blue, "copied native orientation and color");
            Check(TriadObservedCard.Copy(Known with {Owner=0}).Owner is null, "group location does not invent missing color");
            foreach(var raw in new[] {Known with {Top=0},Known with {Right=11},Known with {Stars=6},Known with {Type=5},Known with {Owner=255}})
            {
                var invalid=TriadObservedCard.Copy(raw);
                Check(invalid.Presence==TriadObservedPresence.Incomplete && invalid.Top==0 && invalid.Stars==0, "transition or unsupported field stays incomplete");
            }
        }),
        ("Triad observed frames copy exact slot groups and do not reinterpret UI phase as player turn",()=>
        {
            var blue=Slots(5); var red=Slots(5); var board=Slots(9);
            red[3]=Known with {Owner=2,Top=10}; board[8]=Known with {HasCard=false};
            var frame=new TriadObservedFrame(255,blue,red,board);
            blue[0]=default; red[3]=default;
            Check(frame.BlueHand[0].Top==4 && frame.RedHand[3].Top==10 && frame.Board[8].Presence==TriadObservedPresence.Empty, "all groups and slot positions copied");
            Check(frame.NativePhase==255 && frame.Source.Contains("faces UI",StringComparison.Ordinal), "unknown phase retained, source and limited scope explicit");
            Throws<NotSupportedException>(()=>((IList<TriadObservedCard>)frame.Board).Clear());
            Throws<ArgumentException>(()=>new TriadObservedFrame(1,Slots(4),Slots(5),Slots(9)));
        }),
        ("Triad observation requires opt in and invalidates in flight data after owner disable or disposal",()=>
        {
            using var service=new TriadObservationService();
            Check(!service.State.Enabled && service.BeginRead(10) is null, "no native read lease by default");
            service.SetEnabled(true); var first=service.BeginRead(10)!.Value;
            service.Publish(first,10,1,Slots(5),Slots(5),Slots(9));
            Check(service.State.Frame is not null && service.State.ObservedAt is not null, "accepted owner snapshot");
            var second=service.BeginRead(20)!.Value;
            Check(service.State.Frame is null && service.State.CharacterId==20 && service.State.ObservedAt is null, "owner switch clears immediately");
            service.Publish(first,10,1,Slots(5),Slots(5),Slots(9));
            Check(service.State.Frame is null, "late previous owner rejected");
            service.SetEnabled(false); service.Publish(second,20,1,Slots(5),Slots(5),Slots(9));
            Check(service.State.Status==TriadObservationStatus.Disabled && service.State.Frame is null, "disable clears and rejects late read");
            service.SetEnabled(true); var third=service.BeginRead(20)!.Value;
            service.Publish(second,20,1,Slots(5),Slots(5),Slots(9));
            Check(service.State.Frame is null && third!=second, "re-enable does not revive old ticket for same owner");
            service.Dispose(); service.SetEnabled(true); service.Publish(third,20,1,Slots(5),Slots(5),Slots(9));
            Check(!service.State.Enabled && service.State.Frame is null, "disposed service stays inert");
        }),
        ("Triad observation clears stale frames on logout missing addon failure and malformed publication",()=>
        {
            using var service=new TriadObservationService(); service.SetEnabled(true);
            var ticket=service.BeginRead(10)!.Value;
            void Publish()=>service.Publish(ticket,10,2,Slots(5),Slots(5),Slots(9));
            Publish(); service.Missing(ticket,10,TriadObservationStatus.NotReady);
            Check(service.State.Frame is null && service.State.ObservedAt is null && service.State.Status==TriadObservationStatus.NotReady, "masked addon not last visible frame");
            Publish(); service.Missing(ticket,10,TriadObservationStatus.AddonAbsent);
            Check(service.State.Frame is null && service.State.Status==TriadObservationStatus.AddonAbsent, "closing addon removes last snapshot");
            Publish(); service.Fail(ticket,10,new InvalidOperationException("test-error"));
            Check(service.State.Frame is null && service.State.Error=="test-error", "failed read has no stale data");
            Publish(); Throws<ArgumentException>(()=>service.Publish(ticket,10,1,Slots(5),Slots(5),Slots(8)));
            Check(service.State.Frame is null && service.State.Status==TriadObservationStatus.Error, "malformed snapshot clears previous success");
            Publish(); service.BeginRead(0);
            Check(service.State.Status==TriadObservationStatus.Offline && service.State.Frame is null && service.State.CharacterId==0, "logout clears immediately");
            service.Publish(ticket,10,1,Slots(5),Slots(5),Slots(9));
            Check(service.State.Frame is null, "offline rejects old observation");
            Throws<ArgumentOutOfRangeException>(()=>service.Missing(ticket,10,TriadObservationStatus.Observed));
        }),
    ];
}
