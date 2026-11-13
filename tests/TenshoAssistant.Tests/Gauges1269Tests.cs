using System.Numerics;
using TenshoAssistant.Diagnostics;
using TenshoAssistant.UI;

// 0.1269 (X-264, slice 1, user request of 2026-11-13): the dashboard as gauges. A ring of the last hour (a point every ten seconds), the
// geometry of a gauge and of a curve (pure), the common Gauge and StatTile components, the gauges in the game's dashboard; only measures
// Tensho already takes; nothing drawn while the dashboard is closed.
internal static class Gauges1269Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static HealthSample Sample(long at,int requests=0,double? heap=null)=>new(at,HealthLevel.Ok,heap,null,null,null,0,requests,0,0,0);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gauges 1269: the history keeps one point every ten seconds and the last hour only",() =>
        {
            var history=new HealthHistory();
            Check(history.Count==0 && history.Last is null && history.Due(0),"empty: a point is due");
            Check(history.Add(Sample(1000,heap:10)) && !history.Due(10_999) && history.Due(11_000),"ten seconds between two points");
            Check(!history.Add(Sample(5000,heap:11)) && history.Count==1,"a point too early is not kept");
            for(var index=1;index<500;index++)history.Add(Sample(1000+index*HealthHistory.StepMs,heap:10+index));
            Check(history.Count==HealthHistory.Capacity && HealthHistory.Capacity*HealthHistory.StepMs==3_600_000,"360 points: one hour, never more");
            Check(history[0].HeapMb==10+140 && history.Last!.Value.HeapMb==10+499,"the oldest points left first, order kept");
            var series=history.Series(sample=>sample.HeapMb);
            Check(series.Length==360 && series[0]==150 && series[^1]==509,"series oldest first");
            var threw=false;try{_=history[360];}catch(ArgumentOutOfRangeException){threw=true;}
            Check(threw,"no point outside the ring");
            // The clock went back (TickCount wrapped, or another process's clock on the desktop): the history starts again, nothing mixed.
            Check(history.Due(5) && history.Add(Sample(5,heap:1)) && history.Count==1 && history[0].HeapMb==1,"a clock gone back starts the history again");
        }),
        ("Gauges 1269: a session counter becomes a rate per minute; an unknown stays unknown",() =>
        {
            var history=new HealthHistory();
            Check(history.LastPerMinute(sample=>sample.Requests) is null,"no point: no rate");
            history.Add(Sample(0,requests:4));
            Check(history.LastPerMinute(sample=>sample.Requests) is null && history.PerMinute(sample=>sample.Requests)[0] is null,"one point: nothing to compare");
            history.Add(Sample(10_000,requests:5));history.Add(Sample(20_000,requests:5));history.Add(Sample(30_000,requests:8));
            var rates=history.PerMinute(sample=>sample.Requests);
            Check(rates[1]==6 && rates[2]==0 && rates[3]==18,"1 request in 10 s = 6 a minute; none = 0; 3 = 18");
            Check(history.LastPerMinute(sample=>sample.Requests)==8,"over the points kept (30 s): 4 requests = 8 a minute");
            for(var index=4;index<12;index++)history.Add(Sample(index*10_000,requests:8));
            Check(history.LastPerMinute(sample=>sample.Requests)==0,"over the last minute only: the old requests no longer count");
            history.Add(Sample(120_000,requests:2));
            Check(history.PerMinute(sample=>sample.Requests)[^1] is null,"a counter that went down: unknown, never a negative rate");
        }),
        ("Gauges 1269: fraction, zones and round scales",() =>
        {
            Check(GaugeMath.Fraction(5,10)==0.5f && GaugeMath.Fraction(20,10)==1 && GaugeMath.Fraction(-3,10)==0,"clamped");
            Check(GaugeMath.Fraction(null,10)==0 && GaugeMath.Fraction(double.NaN,10)==0 && GaugeMath.Fraction(5,0)==0,"unknown, not a number, no scale: empty");
            Check(GaugeMath.Zone(3,4,null)==HealthLevel.Ok && GaugeMath.Zone(4,4,null)==HealthLevel.Watch && GaugeMath.Zone(9,4,8)==HealthLevel.Problem,"the rule's thresholds");
            Check(GaugeMath.Zone(null,4,8)==HealthLevel.Ok && GaugeMath.Zone(1000,null,null)==HealthLevel.Ok,"unknown, or no rule: nothing said");
            Check(GaugeMath.NiceMax(0,500)==500 && GaugeMath.NiceMax(730,500)==1000 && GaugeMath.NiceMax(1000,500)==1000 && GaugeMath.NiceMax(1001,500)==2000,"1, 2, 5 times a power of ten, the floor at least");
            Check(GaugeMath.NiceMax(3,10)==10 && GaugeMath.NiceMax(12,10)==20 && GaugeMath.NiceMax(21,10)==50 && GaugeMath.NiceMax(double.NaN,10)==10,"small scales, not a number");
            Check(Math.Abs(HealthHistoryScales.FrameMs-16.6667)<0.001 && HealthBoard.SlowPageMs==4,"the page gauge: a 60 fps frame, worth a look from 4 ms (the dashboard's own rule)");
        }),
        ("Gauges 1269: the dial stays in its box and its arc follows the value",() =>
        {
            foreach(var width in new[]{75f,150f,300f})
            {
                var origin=new Vector2(40,60);
                var (centre,radius,thickness,size)=GaugeMath.Layout(origin,width,18);
                Check(size.X==width && centre.X==origin.X+width/2,"centred in its width");
                var outer=radius+thickness/2;
                Check(centre.X-outer>=origin.X && centre.X+outer<=origin.X+width && centre.Y-outer>=origin.Y+18,"the dial is inside the box, under the title");
                // The dial's lowest points (its two ends, 45° under the horizontal) are inside the box too.
                var low=centre.Y+MathF.Sin(GaugeMath.Start)*outer;
                Check(low<=origin.Y+size.Y+0.01f && low>centre.Y,$"width {width}: the ends of the dial are inside the box, under the centre");
            }
            Check(GaugeMath.Arc(0,1) is (var from,var to,48) && from==GaugeMath.Start && Math.Abs(to-(GaugeMath.Start+GaugeMath.Sweep))<0.0001f,"the whole dial: from bottom left to bottom right, 48 segments");
            Check(MathF.Cos(GaugeMath.Start)<0 && MathF.Sin(GaugeMath.Start)>0 && MathF.Cos(GaugeMath.Start+GaugeMath.Sweep)>0 && MathF.Sin(GaugeMath.Start+GaugeMath.Sweep)>0,"both ends under the centre, left then right");
            Check(GaugeMath.Arc(0,0.5f) is (_,var top,24) && Math.Abs(top-MathF.PI*1.5f)<0.0001f,"half: ends at the top");
            Check(GaugeMath.Arc(0,0) is null && GaugeMath.Arc(0.5f,0.5f) is null && GaugeMath.Arc(0.7f,0.2f) is null,"an empty span draws nothing");
            Check(GaugeMath.Arc(-1,0.01f) is (_,_,2) && GaugeMath.Arc(0.9f,7) is (_,var end,_) && Math.Abs(end-(GaugeMath.Start+GaugeMath.Sweep))<0.0001f,"clamped to the dial; two segments at least");
        }),
        ("Gauges 1269: a curve breaks on an unknown value and never leaves its rectangle",() =>
        {
            var min=new Vector2(10,10);var max=new Vector2(110,60);
            var lines=GaugeMath.Curve([0,5,10,null,20,null,null,7],min,max,10);
            Check(lines.Count==3 && lines[0].Count==3 && lines[1].Count==1 && lines[2].Count==1,"unknown values break the line; lone points stay");
            Check(lines[0][0]==new Vector2(10,60) && lines[0][2].Y==10,"0 at the bottom left, the top at the top");
            Check(lines[1][0].Y==10,"above the scale: clamped at the top");
            Check(lines[2][0].X==110,"the last point at the right edge");
            Check(GaugeMath.Curve([],min,max,10).Count==0 && GaugeMath.Curve([1,2],min,max,0).Count==0,"nothing without values or without a scale");
            Check(GaugeMath.Curve([4],min,max,8)[0][0]==new Vector2(110,35),"a single point: at the right edge");
        }),
        ("Gauges 1269: the frame measures kept for the gauges are the ones already taken",() =>
        {
            var steady=new SteadyFrames();
            Check(steady.LastFrame is null,"nothing before a window has finished");
            steady.Take(0);
            for(var index=0;index<100;index++)steady.Add(SteadyFrames.Frame,index<96 ? 0.4:20);
            Check(steady.Take(SteadyFrames.WindowMs) is not null && steady.LastFrame is { Count:100,Max:20 } last && Math.Abs(last.Average-1.184)<0.001 && last.P95==0.5,"count, average, bound of the 95th percentile, worst");
            var slow=new SlowFrame();
            Check(slow.Report(1,50,0) is null && slow.Reported==0 && slow.Report(2,150,0) is not null && slow.Reported==1,"slow frames counted when reported");
        }),
        ("Gauges 1269: wiring: one reading, a point every ten seconds, gauges only inside the opened dashboard, texts in both languages",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Dashboard.cs");
            Check(page.Contains("internal void TickDashboard(){if(DashboardWanted && DashboardStarted)RefreshDashboard();SampleDashboardHistory();}"),"ticked with the dashboard");
            var sample=page[page.IndexOf("private void SampleDashboardHistory()",StringComparison.Ordinal)..page.IndexOf("private const long DashboardStartDelayMs",StringComparison.Ordinal)];
            Check(sample.Contains("if(!DashboardStarted || !dashboardHistory.Due(now))return;") && sample.Contains("RefreshDashboard();") && !sample.Contains("DashboardInputs()"),"after the start delay, when due, from the dashboard's own reading");
            Check(sample.Contains("dashboardSampleMs=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;"),"the cost of a point is timed");
            var draw=page[page.IndexOf("private void DrawDashboard()",StringComparison.Ordinal)..page.IndexOf("private System.Numerics.Vector4 DashboardColour",StringComparison.Ordinal)];
            Check(draw.IndexOf("UiComponents.Header(",StringComparison.Ordinal)<draw.IndexOf("DrawDashboardGauges(",StringComparison.Ordinal),"gauges only under the opened header");
            Check(page.Split("DrawDashboardGauges(").Length==4,"drawn from one place in game (0.1271: and one in the desktop application)");
            var gauges=page[page.IndexOf("private void DrawDashboardGauges(",StringComparison.Ordinal)..];
            Check(gauges.Contains("HealthHistoryScales.FrameMs,HealthBoard.SlowPageMs,null,"),"the page gauge carries the dashboard's own rule");
            Check(gauges.Contains("if(view.FrameAvgMs is { } frame)") && page.Contains("return new(inputs.ManagedBytes is { } managed ? managed/1048576.0:null,inputs.SlowestMedianMs,inputs.SlowestPage,frame?.Average,frame?.P95,"),"the frame gauge only when the frame is measured (0.1271: through the view)");
            var component=Read("src/TenshoAssistant/UI/Components/Gauges.cs");
            Check(component.Contains("TipText(tip);") && component.Contains("ImGui.InvisibleButton(\"##Gauge\"+id,size)"),"a gauge is reachable and has its help");
            Check(component.Contains("var colour=ruled ? LevelColour(GaugeMath.Zone(amount,watchAt,problemAt)):Accent;"),"a level colour only under a rule");
            Check(!component.Contains("new Vector4("),"theme colours only");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                var section=text[text.IndexOf("\n  \"Dashboard\": {",StringComparison.Ordinal)..];section=section[..section.IndexOf("\n  },",StringComparison.Ordinal)];
                foreach(var key in new[]{"UnitMb","UnitMs","GaugeMemory","GaugeMemoryTip","GaugePage","GaugePageTip","GaugeFrame","GaugeFrameTip","GaugeRequests","GaugeRequestsTip","TileWrites","TileWritesTip","TilePanels","TilePanelsTip","TileReaders","TileReadersTip","TileSlow","TileSlowTip","HistoryCost"})
                    Check(section.Contains("\n    \""+key+"\": \""),$"{file}: Dashboard.{key}");
            }
        }),
    ];
}
