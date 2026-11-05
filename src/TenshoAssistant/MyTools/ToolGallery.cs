using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TenshoAssistant.Community;

namespace TenshoAssistant.MyTools;

public sealed record GalleryEntry(string Id,TemplateText Name,string? Author,string Sha256);
public sealed record GalleryIndex(IReadOnlyList<GalleryEntry>? Entries,TemplateProblem? Problem);
public sealed record GalleryState(bool Loading,IReadOnlyList<GalleryEntry>? Entries,TemplateProblem? Problem,TemplateParse? Opened);

/// <summary>
/// TEMPLATES-001 (DEC-029, T4 to T7): the tools proposed by the community, reviewed then published in this repository (community-templates/). The list and a tool are read only on the player's click,
/// bounded in size. A tool must match the fingerprint of the index, the strict format, its name in the index and the free licence; then it goes through the same preview as a pasted tool.
/// </summary>
public sealed class ToolGallery:IDisposable
{
    public const string Address="https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/community-templates/";
    public const int MaxIndexBytes=256*1024,MaxEntries=500;
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly object gate=new();
    private CancellationTokenSource? running;
    private GalleryState state=new(false,null,null,null);
    private bool disposed;
    public GalleryState State{get{lock(gate)return state;}}

    public ToolGallery(HttpClient? client=null)
    {
        ownsClient=client is null;
        this.client=client ?? new HttpClient{Timeout=TimeSpan.FromSeconds(20)};
    }
    /// <summary>The fingerprint of a tool's file, the same whatever line ends a copy of it has.</summary>
    public static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.TrimStart('﻿').Replace("\r\n","\n")))).ToLowerInvariant();

    public static GalleryIndex ParseIndex(string json)
    {
        try
        {
            using var document=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=8});
            var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("schema",out var schema) || schema.ValueKind!=JsonValueKind.Number || !schema.TryGetInt32(out var version) || version!=1)return new(null,new("Schema"));
            if(!root.TryGetProperty("tools",out var tools) || tools.ValueKind!=JsonValueKind.Array || tools.GetArrayLength()>MaxEntries)return new(null,new("GalleryBroken"));
            var entries=new List<GalleryEntry>();
            foreach(var row in tools.EnumerateArray())
            {
                // Each row is read through a tool's own rules: an empty tool with this identifier and name.
                if(row.ValueKind!=JsonValueKind.Object || !row.TryGetProperty("id",out var id) || !row.TryGetProperty("name",out var name) || !row.TryGetProperty("sha256",out var hash)
                    || id.ValueKind!=JsonValueKind.String || hash.ValueKind!=JsonValueKind.String || hash.GetString() is not {Length:64} fingerprint || !fingerprint.All(Uri.IsHexDigit))return new(null,new("GalleryBroken"));
                var checkedName=ToolTemplateFormat.Parse($$"""{"schema":1,"id":{{id.GetRawText()}},"name":{{name.GetRawText()}},"blocks":[]}""");
                if(checkedName.Template is not { } tool)return new(null,new("GalleryBroken"));
                var author=row.TryGetProperty("author",out var writer) && writer.ValueKind==JsonValueKind.String ? ToolTemplateFormat.Clean(writer.GetString()):"";
                if(author.Length>ToolTemplateFormat.MaxAuthor)return new(null,new("GalleryBroken"));
                entries.Add(new(tool.Id,tool.Name,author.Length>0 ? author:null,fingerprint.ToLowerInvariant()));
            }
            return new(entries,null);
        }
        catch(JsonException){return new(null,new("GalleryBroken"));}
    }
    /// <summary>What a downloaded tool must be: the file the index names, valid, under the free licence.</summary>
    public static TemplateParse Verify(GalleryEntry entry,string text)
    {
        if(Hash(text)!=entry.Sha256)return new(null,new("GalleryMismatch"));
        var parse=ToolTemplateFormat.Parse(text);
        if(parse.Template is not { } template)return parse;
        if(template.Id!=entry.Id)return new(null,new("GalleryMismatch"));
        return template.License==ToolDraft.FreeLicense ? parse:new(null,new("GalleryLicense"));
    }

    private CancellationToken Start(GalleryState next)
    {
        lock(gate)
        {
            running?.Cancel();running?.Dispose();
            running=new();state=next;
            return running.Token;
        }
    }
    private void Finish(CancellationToken token,Func<GalleryState,GalleryState> change)
    {
        lock(gate)if(!disposed && !token.IsCancellationRequested)state=change(state);
    }
    public Task LoadListAsync()
    {
        if(disposed)return Task.CompletedTask;
        var token=Start(new(true,null,null,null));
        return Task.Run(async()=>
        {
            try
            {
                var index=ParseIndex(await BoundedHttp.ReadAsync(client,new Uri(Address+"index.json"),MaxIndexBytes,token).ConfigureAwait(false));
                Finish(token,_=>new(false,index.Entries,index.Problem,null));
            }
            catch(Exception error) when(error is HttpRequestException or TaskCanceledException or InvalidDataException or DecoderFallbackException)
            {Finish(token,_=>new(false,null,new("GalleryUnavailable"),null));}
        });
    }
    public Task OpenAsync(GalleryEntry entry)
    {
        if(disposed || !ToolTemplateFormat.ValidId(entry.Id))return Task.CompletedTask;
        var list=State.Entries;
        var token=Start(new(true,list,null,null));
        return Task.Run(async()=>
        {
            try
            {
                var parse=Verify(entry,await BoundedHttp.ReadAsync(client,new Uri($"{Address}tools/{entry.Id}.json"),ToolTemplateFormat.MaxBytes+4096,token).ConfigureAwait(false));
                Finish(token,_=>new(false,list,parse.Problem,parse.Problem is null ? parse:null));
            }
            catch(Exception error) when(error is HttpRequestException or TaskCanceledException or InvalidDataException or DecoderFallbackException)
            {Finish(token,_=>new(false,list,new("GalleryUnavailable"),null));}
        });
    }
    /// <summary>Forgets the opened tool, once it is installed or closed.</summary>
    public void CloseTool(){lock(gate)state=state with {Opened=null,Problem=null};}
    public void Dispose()
    {
        lock(gate)
        {
            if(disposed)return;
            disposed=true;running?.Cancel();running?.Dispose();running=null;
        }
        if(ownsClient)client.Dispose();
    }
}
