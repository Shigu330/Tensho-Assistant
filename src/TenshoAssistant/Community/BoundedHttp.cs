using System.Net.Http;
using System.Text;

namespace TenshoAssistant.Community;

/// <summary>
/// Community requests read with a size limit: headers first, the declared length checked, then the body read by blocks and stopped as
/// soon as it passes the limit (a large reply is never loaded whole). Every request carries Tensho's User-Agent.
/// </summary>
public static class BoundedHttp
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string,int> SentByHost=new();
    /// <summary>Requests sent so far, by host: the page sweep of the self-check says how many it caused (0.724).</summary>
    public static IReadOnlyDictionary<string,int> Sent=>new Dictionary<string,int>(SentByHost);
    /// <summary>X-76 (step 2): Tensho's one User-Agent, « TenshoAssistant/x.y.z », for every request it sends (the market clients and Discord use it too).</summary>
    public static readonly string UserAgent=$"TenshoAssistant/{typeof(BoundedHttp).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";

    public static Task<string> ReadAsync(HttpClient client,Uri uri,int maximumBytes,CancellationToken cancellation)=>
        SendAsync(client,new HttpRequestMessage(HttpMethod.Get,uri),maximumBytes,cancellation);

    /// <summary>Sends any request (0.321: POST for Teamcraft's GraphQL) and reads its reply within <paramref name="maximumBytes"/>.</summary>
    public static async Task<string> SendAsync(HttpClient client,HttpRequestMessage request,int maximumBytes,CancellationToken cancellation)
    {
        using var message=request;
        if(maximumBytes<=0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        SentByHost.AddOrUpdate(message.RequestUri?.Host ?? "?",1,(_,count)=>count+1);
        message.Headers.UserAgent.ParseAdd(UserAgent);
        using var response=await client.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,cancellation).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>maximumBytes) throw new InvalidDataException("Community response too large.");
        await using var stream=await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
        using var buffer=new MemoryStream(); var bytes=new byte[8192]; int read;
        while((read=await stream.ReadAsync(bytes,cancellation).ConfigureAwait(false))>0)
        {
            if(buffer.Length+read>maximumBytes) throw new InvalidDataException("Community response too large.");
            buffer.Write(bytes,0,read);
        }
        cancellation.ThrowIfCancellationRequested();
        return new UTF8Encoding(false,true).GetString(buffer.GetBuffer(),0,(int)buffer.Length);
    }
}
