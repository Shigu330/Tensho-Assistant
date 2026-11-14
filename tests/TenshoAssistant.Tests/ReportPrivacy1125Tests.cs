using TenshoAssistant.Diagnostics;
using TenshoAssistant.Help;

// 0.1125 (X-143 part 3, audit A08): the problem report and the self-check mask a path whole (folders with spaces, network paths), the
// Windows user and computer names, and the browser opens only after a preview made of the very strings put in the address and the
// clipboard. Behaviour test (R28): a real file error in a folder with a space, a real address decoded field by field and compared to the
// preview, a too long report sent to the clipboard.
internal static class ReportPrivacy1125Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly string[] Names=["Jane Doe","DESKTOP-JD42"];

    private static List<(string Key,string Value)> Query(string url)
    {
        var query=url[(url.IndexOf('?')+1)..];
        return query.Split('&').Select(pair=>pair.Split('=',2)).Where(pair=>pair[0]!="template").Select(pair=>(pair[0],Uri.UnescapeDataString(pair[1]))).ToList();
    }

    private static void NothingPrivate(string text,string where)
    {
        foreach(var secret in new[]{"Jane","Doe","DESKTOP-JD42","NAS-01","Users\\","Users/","share","discord.com/api"})
            Check(!text.Contains(secret,StringComparison.OrdinalIgnoreCase),$"{where}: '{secret}' left in « {text} »");
    }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Report privacy: a path is masked whole, folders with spaces and network paths included",() =>
        {
            var masked=ProblemReport.Clean("Could not open C:\\Users\\Jane Doe\\AppData\\Roaming\\XIVLauncher\\pluginConfigs\\x.json now",Names);
            Check(masked=="Could not open <path> now",masked);
            Check(ProblemReport.Clean("see \\\\NAS-01\\share\\Jane Doe\\log.txt end",Names)=="see <path> end","network path");
            Check(ProblemReport.Clean("long \\\\?\\C:\\Users\\Jane Doe\\a.txt",Names).Contains("<path>") && !ProblemReport.Clean("\\\\?\\C:\\Users\\Jane Doe\\a.txt",Names).Contains("Doe"),"long path");
            Check(ProblemReport.Clean("file:///D:/Games/Jane Doe/x.dat",Names)=="file:///<path>","forward slashes");
            Check(ProblemReport.Clean("at https://github.com/Shigu330/Tensho-Assistant ok",Names)=="at https://github.com/Shigu330/Tensho-Assistant ok","an address is not a drive");
            Check(ProblemReport.Clean("PC DESKTOP-JD42 for jane doe",Names)=="PC <user> for <user>","names, any case");
            Check(ProblemReport.Clean("PC is ok",["PC"])=="PC is ok","a name of 2 letters is left");
        }),
        ("Report privacy: a real file error in a folder with a space, through the self-check",() =>
        {
            var root=Path.Combine(Path.GetTempPath(),"tensho-1125-"+Guid.NewGuid().ToString("N"),"Jane Doe","pluginConfigs");
            Directory.CreateDirectory(root);
            try
            {
                string message;
                try { File.ReadAllText(Path.Combine(root,"missing file.json")); message=""; }
                catch(Exception error) { message=error.Message; }
                Check(message.Contains("Jane Doe"),"the real error names the folder");
                var line=SelfCheck.Clean(message);
                Check(!line.Contains("Jane") && !line.Contains("Doe") && line.Contains("<path>"),line);
                var report=ProblemReport.Diagnostic([("Items",message)],Names);
                Check(!report.Contains("Jane") && report.StartsWith("Items: ",StringComparison.Ordinal),report);
            }
            finally { Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(root))!,true); }
        }),
        ("Report privacy: the diagnosis keeps only its listed fields, one line each, bounded",() =>
        {
            var text=ProblemReport.Diagnostic([("Tensho","0.1125.0 · fr · page Map"),("Character","Jane Doe of Ragnarok"),("Maps","12\nC:\\Users\\Jane Doe\\x"),("Alarms",new string('x',900))],Names);
            Check(!text.Contains("Character") && !text.Contains("Ragnarok"),"an unknown field is dropped");
            Check(text.Contains("Maps: 12 <path>\n"),"one line, path masked: "+text);
            Check(text.Split('\n').All(line=>line.Length<=260),"bounded");
        }),
        ("Report privacy: the preview is the address field by field, and the clipboard text",() =>
        {
            var draft=new ProblemDraft(ProblemKind.Bug,"Crash in C:\\Users\\Jane Doe\\x","I opened \\\\NAS-01\\share\\Jane Doe\\a b.txt","It works","Selftest Items OK\nDESKTOP-JD42","0.1125.0","fr");
            var send=ProblemReport.Prepare(draft,Names);
            Check(send.Clipboard is null,"short: all in the address");
            var query=Query(send.Url);
            Check(query.SequenceEqual(send.Fields),"the fields are the address's own strings");
            var preview=send.Preview();
            foreach(var (key,value) in query) Check(preview.Contains("["+key+"]\n"+value,StringComparison.Ordinal),"preview shows "+key);
            NothingPrivate(send.Url,"address");
            NothingPrivate(Uri.UnescapeDataString(send.Url),"decoded address");
            NothingPrivate(preview,"preview");
            var big=new string('y',7000)+" C:\\Users\\Jane Doe\\big.log";
            var longer=ProblemReport.Prepare(draft with { Report=big },Names);
            Check(longer.Clipboard is { } clipboard && clipboard.Contains("<path>") && longer.Url.Length<6000 && !longer.Url.Contains("report="),"the report goes to the clipboard");
            NothingPrivate(longer.Clipboard!,"clipboard");
            Check(longer.Preview().EndsWith(longer.Clipboard!,StringComparison.Ordinal),"the preview shows the clipboard text too");
            Check(Query(longer.Url).SequenceEqual(longer.Fields),"shorter address, same rule");
            var link=ProblemReport.Link(draft);
            Check(!link.ReportLeftOut && link.Url.Contains("template=bug_report.yml"),"Link stays the same address");
        }),
        ("Report privacy: the browser opens from the preview only, and the diagnosis is built from fields",() =>
        {
            var help=Read("src/TenshoAssistant/MainWindow.Help.cs");
            Check(help.Contains("Dalamud.Utility.Util.OpenLink(preview.Send.Url)") && !help.Contains("OpenLink(link.Url)"),"opens the previewed address");
            Check(help.Contains("if (ProblemDraftNow(kind) != preview.Draft || preview.Direct != direct) problemPreview = null;"),"a changed draft drops the preview");
            Check(help.Contains("if (preview.Send.Clipboard is { } clipboard) ImGui.SetClipboardText(clipboard);"),"the clipboard gets the previewed text");
            var advanced=Read("src/TenshoAssistant/MainWindow.Advanced.cs");
            Check(advanced.Contains("private string BugReport() => TenshoAssistant.Help.ProblemReport.Diagnostic(BugReportFields());"),"diagnosis from fields");
            var self=Read("src/TenshoAssistant/Diagnostics/SelfCheck.cs");
            Check(self.Contains("TenshoAssistant.Help.ProblemReport.Clean(") && !self.Contains("PathPattern()"),"the self-check uses the same cleaning");
        }),
    ];
}
