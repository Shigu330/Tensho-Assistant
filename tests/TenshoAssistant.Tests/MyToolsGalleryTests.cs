using System.Net;
using System.Net.Http;
using System.Text;
using TenshoAssistant.MyTools;

// TEMPLATES-001 (T4 to T7): the gallery in this repository and its reading by the plugin.
internal static class MyToolsGalleryTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");
    private static void Wait(Task task)=>task.GetAwaiter().GetResult();
    private const string Valid="""{"schema":1,"id":"sample-tool","name":{"en":"Sample"},"license":"CC0-1.0","blocks":[{"type":"note","text":{"en":"a"}}]}""";
    private static string Index(string text,string id="sample-tool")=>$$"""{"schema":1,"tools":[{"id":"{{id}}","name":{"en":"Sample"},"author":"Ann","sha256":"{{ToolGallery.Hash(text)}}"}]}""";

    private sealed class Handler(Func<string,HttpResponseMessage> answer):HttpMessageHandler
    {
        public readonly List<string> Asked=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            lock(Asked)Asked.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(answer(request.RequestUri!.AbsoluteUri));
        }
    }
    private static HttpResponseMessage Ok(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text,Encoding.UTF8)};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gallery in the repository: every tool passes the plugin's rules, under the free licence, named after its id, and the index matches",() =>
        {
            var folder=Path.GetFullPath("community-templates/tools");
            var files=Directory.GetFiles(folder,"*.json");
            Check(files.Length>=2,"sample tools");
            var index=ToolGallery.ParseIndex(File.ReadAllText(Path.GetFullPath("community-templates/index.json")));
            Check(index.Entries is not null && index.Entries.Count==files.Length,"run scripts/Update-ToolGallery.ps1: "+index.Problem?.Code);
            foreach(var file in files)
            {
                var text=File.ReadAllText(file);
                var id=Path.GetFileNameWithoutExtension(file);
                var entry=index.Entries!.SingleOrDefault(row=>row.Id==id);
                Check(entry is not null,"not in the index: "+id);
                var parse=ToolGallery.Verify(entry!,text);
                Check(parse.Template is not null,$"{id}: {parse.Problem?.Code} {parse.Problem?.Where}; run scripts/Update-ToolGallery.ps1 if the file changed");
                Check(parse.Template!.Name.Values.ContainsKey("en"),"an English name: "+id);
            }
        }),
        ("Gallery fingerprint: the same whatever the line ends; another file is refused, as an id or licence that do not match",() =>
        {
            Check(ToolGallery.Hash("a\nb")==ToolGallery.Hash("a\r\nb") && ToolGallery.Hash("a")==ToolGallery.Hash("﻿a") && ToolGallery.Hash("a")!=ToolGallery.Hash("b"),"fingerprint");
            var entry=new GalleryEntry("sample-tool",ToolDraft.Empty,null,ToolGallery.Hash(Valid));
            Check(ToolGallery.Verify(entry,Valid).Template is not null,"accepted");
            Check(ToolGallery.Verify(entry,Valid+" ").Problem?.Code=="GalleryMismatch","changed file");
            var other=Valid.Replace("sample-tool","other-tool");
            Check(ToolGallery.Verify(entry with {Sha256=ToolGallery.Hash(other)},other).Problem?.Code=="GalleryMismatch","another tool");
            var closed=Valid.Replace("CC0-1.0","MIT");
            Check(ToolGallery.Verify(entry with {Sha256=ToolGallery.Hash(closed)},closed).Problem?.Code=="GalleryLicense","licence");
            var broken=Valid.Replace("\"note\"","\"macro\"");
            Check(ToolGallery.Verify(entry with {Sha256=ToolGallery.Hash(broken)},broken).Problem?.Code=="UnknownBlock","the format's rules also apply");
        }),
        ("Gallery index: strict; a row that a tool could not have refuses the list",() =>
        {
            Check(ToolGallery.ParseIndex(Index(Valid)).Entries is {Count:1},"valid");
            foreach(var bad in new[]{"x","[]","""{"schema":2,"tools":[]}""","""{"schema":1}""",Index(Valid,"../../x"),"""{"schema":1,"tools":[{"id":"sample-tool","name":{"en":"a"},"sha256":"zz"}]}""","""{"schema":1,"tools":[{"id":"sample-tool","sha256":"00"}]}"""})
                Check(ToolGallery.ParseIndex(bad).Entries is null,"refused: "+bad);
        }),
        ("Gallery reading: nothing at construction; list and tool on request; failures are said, never thrown",() =>
        {
            var handler=new Handler(address=>address.EndsWith("index.json") ? Ok(Index(Valid)):address.EndsWith("/tools/sample-tool.json") ? Ok(Valid):new(HttpStatusCode.NotFound));
            using var gallery=new ToolGallery(new HttpClient(handler));
            Check(handler.Asked.Count==0 && gallery.State.Entries is null,"no request until a click");
            Wait(gallery.LoadListAsync());
            Check(gallery.State is {Loading:false,Entries.Count:1} && handler.Asked.Single()==ToolGallery.Address+"index.json","list");
            Wait(gallery.OpenAsync(gallery.State.Entries![0]));
            Check(gallery.State.Opened?.Template?.Id=="sample-tool" && gallery.State.Entries is {Count:1} && handler.Asked[1]==ToolGallery.Address+"tools/sample-tool.json","tool");
            gallery.CloseTool();Check(gallery.State.Opened is null && gallery.State.Entries is {Count:1},"closed");
            Wait(gallery.OpenAsync(new("missing-tool",ToolDraft.Empty,null,new string('0',64))));
            Check(gallery.State.Problem?.Code=="GalleryUnavailable" && gallery.State.Opened is null,"not found");
            var calls=handler.Asked.Count;
            Wait(gallery.OpenAsync(new("../../x",ToolDraft.Empty,null,new string('0',64))));
            Check(handler.Asked.Count==calls,"an id that is not valid builds no address");
            using var tampered=new ToolGallery(new HttpClient(new Handler(address=>address.EndsWith("index.json") ? Ok(Index(Valid)):Ok(Valid+" "))));
            Wait(tampered.LoadListAsync());Wait(tampered.OpenAsync(tampered.State.Entries![0]));
            Check(tampered.State.Problem?.Code=="GalleryMismatch" && tampered.State.Opened is null,"tampered tool");
            using var large=new ToolGallery(new HttpClient(new Handler(_=>Ok(new string(' ',ToolGallery.MaxIndexBytes+1)))));
            Wait(large.LoadListAsync());Check(large.State.Problem?.Code=="GalleryUnavailable","too large");
            using var down=new ToolGallery(new HttpClient(new Handler(_=>throw new HttpRequestException("down"))));
            Wait(down.LoadListAsync());Check(down.State is {Loading:false,Entries:null} && down.State.Problem?.Code=="GalleryUnavailable","no network");
        }),
        ("Gallery wiring: read on a click only, shown through the same preview; declared in the manifest and the credits",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.MyTools.cs");
            Check(page.Contains("if(ImGui.SmallButton(localization.Get(\"MyToolsUi.GalleryLoad\")))_=MyToolsGallery.LoadListAsync();") && page.Contains("DrawMyToolPreview(store,opened)"),"on a click, same preview");
            Check(Read("src/TenshoAssistant/Credits/CreditCatalog.cs").Contains("new(\"ToolGallery\", CreditKind.Online,") && Read("src/TenshoAssistant/TenshoAssistant.json").Contains("community tools"),"declared");
            Check(Read("community-templates/README.md").Contains("CC0-1.0") && Read("scripts/Update-ToolGallery.ps1").Contains("index.json"),"how to propose");
        }),
    ];
}
