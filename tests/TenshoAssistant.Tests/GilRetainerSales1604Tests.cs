using TenshoAssistant.Inventory;
using TenshoAssistant.Localization;

// 0.1604 (X-658, the user's report of 20/11: « I received gil from the market and it wasn't counted »). Behaviour tests (R28.14, the user's
// data): the journal's balances replayed as the user's journal of 20/11 recorded them (docs/evidence/0.1604-gil-retainer-sales.md): a reload
// while a retainer holds its sales, then the withdrawal; the one-reading drop of a retainer's purse when another one opens; a deposit then
// sales. Every market sale received by a retainer is counted once under « Market », the transfer is the same on both sides.
internal static class GilRetainerSales1604Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset Start=new(2026,11,20,10,28,0,TimeSpan.Zero);

    // The character's changes: a withdrawal or a deposit while the retainer's window is open.
    private static GilCategory Bank(long delta)=>GilCategory.Retainer;
    private static long Sum(List<GilEntry> entries,GilSource source,GilCategory category,int sign)
        =>entries.Where(entry=>entry.Source==source && entry.Category==category && Math.Sign(entry.Delta)==sign).Sum(entry=>Math.Abs(entry.Delta));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil retainer sales 1604: the user's 20/11: sales, withdrawal, a reload with a retainer open, withdrawal: both sales under Market, transfers equal",()=>
        {
            var entries=new List<GilEntry>();var at=Start;
            var before=new GilRecorder();
            void Read(GilRecorder recorder,long gil,long retainers){recorder.Read(gil,retainers,null,at,entries,Bank);at=at.AddSeconds(1);}
            // The session's start: the retainers' list shows 0 (both emptied on 17/11), the character 97 213.
            Read(before,97213,0);Read(before,97213,0);
            // The first retainer opened: its purse holds 110 765 of sales; withdrawn.
            Read(before,97213,110765);Read(before,97213,110765);
            Read(before,207978,0);Read(before,207978,0);
            // The plugin reloads (a new version) while the second retainer is open with 215 005 of sales: a new watcher, the journal loaded.
            var after=new GilRecorder();
            after.Seed(entries.ToList());
            Check(after.Retainers==0,"the retainers' total starts from the journal's last balance");
            Read(after,207978,215005);Read(after,207978,215005);
            Read(after,422983,0);Read(after,422983,0);
            Check(Sum(entries,GilSource.Retainers,GilCategory.Market,1)==110765+215005,"both sales under Market: "+Sum(entries,GilSource.Retainers,GilCategory.Market,1));
            Check(Sum(entries,GilSource.Retainers,GilCategory.Retainer,-1)==325770 && Sum(entries,GilSource.Character,GilCategory.Retainer,1)==325770,"the transfer is the same on both sides");
            // Without the journal's balance (a character with no retainer entry yet): a starting point, as before.
            var fresh=new GilRecorder();fresh.Seed([]);
            Check(fresh.Retainers is null,"nothing recorded: no guessed zero (R06)");
        }),
        ("Gil retainer sales 1604: a one-reading drop of the total (another retainer opened, purse not filled yet) is neither a withdrawal nor a sale",()=>
        {
            var entries=new List<GilEntry>();var at=Start;var recorder=new GilRecorder();
            foreach(var total in new long[]{1527,1527,528,1527,1527,1998,1527,1527})
            {recorder.Read(77699,total,null,at,entries,Bank);at=at.AddSeconds(1);}
            Check(entries.Count==0 && recorder.Retainers==1527,"no entry for readings that do not hold: "+entries.Count);
            // A real change holds two readings: recorded once, one second later.
            recorder.Read(77699,2526,null,at,entries,Bank);
            Check(entries.Count==0,"read once: not yet");
            recorder.Read(77699,2526,null,at.AddSeconds(1),entries,Bank);
            Check(entries.Count==1 && entries[0] is {Delta:999,Category:GilCategory.Market,Balance:2526},"read twice: a sale of 999");
            recorder.Read(77699,null,null,at.AddSeconds(2),entries,Bank);
            Check(entries.Count==1 && recorder.Retainers==2526,"unknown: nothing changes");
        }),
        ("Gil retainer sales 1604: a deposit then the retainers' rise: the deposit a transfer, the rest a sale; withdrawal not counted as a sale",()=>
        {
            var entries=new List<GilEntry>();var at=Start;var recorder=new GilRecorder();
            void Read(long gil,long retainers){recorder.Read(gil,retainers,null,at,entries,Bank);at=at.AddSeconds(1);}
            Read(10000,1000);Read(10000,1000);
            Read(9500,1000);Read(9500,1000);
            Read(9500,2200);Read(9500,2200);
            Check(Sum(entries,GilSource.Retainers,GilCategory.Retainer,1)==500 && Sum(entries,GilSource.Retainers,GilCategory.Market,1)==700,"500 deposited, 700 sold");
            Read(11700,0);Read(11700,0);
            Check(Sum(entries,GilSource.Retainers,GilCategory.Market,1)==700 && Sum(entries,GilSource.Retainers,GilCategory.Retainer,-1)==2200 && Sum(entries,GilSource.Character,GilCategory.Retainer,1)==2200,"the withdrawal: a transfer only");
            recorder.Reset();
            Check(recorder.Gil is null && recorder.Retainers is null && recorder.Company is null,"another character: nothing known");
        }),
        ("Gil retainer sales 1604: one way of writing the amounts; the amounts' titles on the right; the watcher seeds and settles through GilRecorder",()=>
        {
            Check(TenshoCulture.Signed(-62360)==(-62360L).ToString("+#,0;-#,0;0",TenshoCulture.Current) && TenshoCulture.Signed(0)=="0","signed, in the language's culture");
            Check(TenshoCulture.For("fr") is var fr && (-62360L).ToString("+#,0;-#,0;0",fr)=="-62 360" && (327297L).ToString("N0",fr)=="327 297","French: the same group separator in both writings");
            string Read(string path)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+path)).Replace("\r\n","\n");
            var page=Read("MainWindow.GilJournal.cs");
            Check(!page.Contains("\"+#,0;-#,0") && page.Split("TenshoCulture.Signed(").Length==4,"no hand-written signed format on the page");
            Check(page.Contains("UI.UiComponents.HeadersRow(titles,column=>column>0);") && page.Contains("UI.UiComponents.HeadersRow(titles,column=>column is 1 or 2);") && !page.Contains("ImGui.TableHeadersRow();"),"titles above their amounts");
            var watcher=Read("Inventory/GilWatcher.Dalamud.cs");
            Check(watcher.Contains("if(read.IsCompletedSuccessfully){entries=read.Result;recorder.Seed(entries);}") && watcher.Contains("character=id;recorder.Reset();")
                && watcher.Contains("var changed=recorder.Read(newGil,newRetainers,newCompany,now,entries,Category,"),"seeded at load, reset at a switch, read through GilRecorder");
        }),
    ];
}
