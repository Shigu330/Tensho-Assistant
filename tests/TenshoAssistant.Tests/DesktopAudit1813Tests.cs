using TenshoAssistant.Desktop;
using TenshoAssistant.Link;

// 0.1813 (X-592, external audit of 18/11, desktop app). Behaviour tests (R28.14): A21 an unreadable update.json never turns the automatic
// update back on, and « off » is not said when the setting could not be written; A22 a copy of the discoveries removed between two refreshes
// clears the old reading, and a late older reading never comes back over a newer one.
internal static class DesktopAudit1813Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-audit-1813-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{run(root);}finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static string Source(string file)
    {
        var dir=AppContext.BaseDirectory;
        while(dir is not null && !Directory.Exists(Path.Combine(dir,"src")))dir=Path.GetDirectoryName(dir);
        return File.ReadAllText(Path.Combine(dir!,"src",file));
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop audit 1813 A21: an unreadable update.json reads as automatic update off, the check at start does not run, nothing is written over it",() => InTemp(root =>
        {
            var own=Path.Combine(root,"own");Directory.CreateDirectory(own);
            File.WriteAllText(DesktopInstall.StatePath(own),"{ \"AutoUpdate\": tr");
            var bytes=File.ReadAllBytes(DesktopInstall.StatePath(own));
            var state=DesktopInstall.Load(own);
            Check(DesktopInstall.LoadReadable(own) is null && !state.AutoUpdate && state.Current is null,"unreadable: automatic update off, no copy known");
            Check(DesktopUpdate.CheckRefusal(readable:false,state.AutoUpdate,manual:false)=="Desktop.UpdateUnreadable","the check at start refused with its own status");
            Check(DesktopUpdate.CheckRefusal(readable:false,autoUpdate:true,manual:false)=="Desktop.UpdateUnreadable","even if the setting read on before");
            Check(DesktopUpdate.CheckRefusal(readable:false,state.AutoUpdate,manual:true) is null,"the button still checks");
            var refused=false;
            try{DesktopInstall.Update(own,s=>s with {AutoUpdate=true},DesktopInstall.ClickWaitMs);}catch(IOException){refused=true;}
            Check(refused && File.ReadAllBytes(DesktopInstall.StatePath(own)).SequenceEqual(bytes),"turning it on is refused, the file untouched");
            Check(DesktopUpdate.AutoStatus(on:true,saved:false,busy:false)=="Desktop.UpdateNotSaved","then the status says « not saved »");
        })),
        ("Desktop audit 1813 A21: a missing update.json is a new install (on); a readable one is followed",() => InTemp(root =>
        {
            var own=Path.Combine(root,"own");
            Check(DesktopInstall.Load(own).AutoUpdate && DesktopUpdate.CheckRefusal(true,true,false) is null,"missing: on, the check at start runs");
            DesktopInstall.Update(own,s=>s with {AutoUpdate=false});
            Check(!DesktopInstall.Load(own).AutoUpdate && DesktopUpdate.CheckRefusal(true,false,false)=="Desktop.UpdateOff","off: the check at start does not run");
        })),
        ("Desktop audit 1813 A21: « off » only when saved; a write that fails says « not saved », even when turning off",() =>
        {
            Check(DesktopUpdate.AutoStatus(on:false,saved:false,busy:false)=="Desktop.UpdateNotSaved","turning off not saved: never « off »");
            Check(DesktopUpdate.AutoStatus(on:false,saved:false,busy:true)=="Desktop.UpdateNotSaved","not saved is said even during a check");
            Check(DesktopUpdate.AutoStatus(on:false,saved:true,busy:false)=="Desktop.UpdateOff","saved off: « off »");
            Check(DesktopUpdate.AutoStatus(on:true,saved:true,busy:false)=="Desktop.UpdateOn","saved on: « on »");
            Check(DesktopUpdate.AutoStatus(on:true,saved:true,busy:true) is null,"a running check keeps its status");
        }),
        ("Desktop audit 1813 A21: the held lock of the other side (plugin) gives « not saved », the state kept",() => InTemp(root =>
        {
            var own=Path.Combine(root,"own");
            DesktopInstall.Update(own,s=>s with {Skipped="7"});
            using(new FileStream(DesktopInstall.LockPath(own),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                var saved=true;
                try{DesktopInstall.Update(own,s=>s with {AutoUpdate=false},DesktopInstall.ClickWaitMs);}catch(IOException){saved=false;}
                Check(!saved && DesktopUpdate.AutoStatus(false,saved,false)=="Desktop.UpdateNotSaved","busy lock: not saved");
            }
            var state=DesktopInstall.Load(own);
            Check(state.AutoUpdate && state.Skipped=="7","the state on disk unchanged");
        })),
        ("Desktop audit 1813 A22: a reading begun before a newer one is never shown; « copy missing » set in between stays",() =>
        {
            var slot=new DesktopCopySlot();
            string? shown="old reading";
            var older=slot.Begin();
            var missing=slot.Begin();
            Check(slot.Publish(missing,()=>shown=null),"the copy removed: what was shown is cleared");
            Check(!slot.Publish(older,()=>shown="old reading") && shown is null,"the older reading finishing late does not come back");
            var newer=slot.Begin();
            Check(slot.Publish(newer,()=>shown="new reading") && shown=="new reading","the next copy is shown");
        }),
        ("Desktop audit 1813 A22: two readings at once, many times: only the newest is ever left shown",() =>
        {
            for(var round=0;round<200;round++)
            {
                var slot=new DesktopCopySlot();
                var shown=-1;
                var a=slot.Begin();
                var late=Task.Run(()=>slot.Publish(a,()=>shown=a));
                var b=slot.Begin();
                slot.Publish(b,()=>shown=b);
                late.Wait();
                Check(shown==b,"round "+round+": the newest reading is the one left shown ("+shown+")");
            }
        }),
        ("Desktop audit 1813: wiring: the updater follows the rules, the copy's discoveries go through the slot and the page says why",() =>
        {
            var updater=Source("TenshoAssistant.Desktop/DesktopUpdater.cs");
            Check(updater.Contains("DesktopUpdate.CheckRefusal(readable,State.AutoUpdate,manual)") && updater.Contains("DesktopUpdate.AutoStatus(on,saved,"),"the updater uses the rules");
            Check(!updater.Contains("catch(IOException){}"),"no failed write swallowed");
            var data=Source("TenshoAssistant/MainWindow.DesktopData.cs");
            Check(data.Contains("\"DiscoveriesUi.DesktopCopyMissing\"") && data.Contains("\"DiscoveriesUi.DesktopCopyUnreadable\"") && !data.Contains("if(!File.Exists(file))return;"),"no copy and an unreadable copy clear the old reading");
            Check(Source("TenshoAssistant/MainWindow.Discoveries.cs").Contains("DesktopDiscoveriesProblem is { } copyProblem"),"the page says why");
        }),
    ];
}
