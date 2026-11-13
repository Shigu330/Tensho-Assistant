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
    /// <summary>X-165 (0.1247): a request sent by a client that does not go through this reader (the market prices of Universalis, Discord),
    /// counted with the others for the dashboard; the host only, never the path (a Discord webhook's path is a secret, R24).</summary>
    public static void Count(string host)=>SentByHost.AddOrUpdate(host,1,(_,count)=>count+1);
    /// <summary>X-76 (step 2): Tensho's one User-Agent, « TenshoAssistant/x.y.z », for every request it sends (the market clients and Discord use it too).</summary>
    public static readonly string UserAgent=$"TenshoAssistant/{typeof(BoundedHttp).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";

    /// <summary>
    /// X-143 (0.1123, audit A06): the longest a request may take, headers and body together, when its client has no shorter timeout. With
    /// ResponseHeadersRead, HttpClient.Timeout stops at the headers: a reply that stops coming would otherwise hang its page for good.
    /// </summary>
    public static readonly TimeSpan DefaultDeadline=TimeSpan.FromMinutes(2);

    public static Task<string> ReadAsync(HttpClient client,Uri uri,int maximumBytes,CancellationToken cancellation)=>
        SendAsync(client,new HttpRequestMessage(HttpMethod.Get,uri),maximumBytes,cancellation);

    /// <summary>The deadline of a request: the client's timeout when it is finite and shorter, else DefaultDeadline.</summary>
    public static TimeSpan Deadline(HttpClient client)=>client.Timeout>TimeSpan.Zero && client.Timeout<DefaultDeadline ? client.Timeout : DefaultDeadline;

    /// <summary>
    /// Sends any request (0.321: POST for Teamcraft's GraphQL) and reads its reply within <paramref name="maximumBytes"/>. X-143 (0.1123):
    /// within Deadline(client) too, body included; past it, a TaskCanceledException holding a TimeoutException (as HttpClient's own
    /// timeout); the caller's own cancellation stays an OperationCanceledException of its token.
    /// </summary>
    public static async Task<string> SendAsync(HttpClient client,HttpRequestMessage request,int maximumBytes,CancellationToken cancellation,TimeSpan? deadline=null)
        =>new UTF8Encoding(false,true).GetString(await ReceiveAsync(client,request,maximumBytes,cancellation,deadline).ConfigureAwait(false));

    /// <summary>X-174 (0.1137): the same bounded read of a GET, its bytes as they came (a compressed file of the shared discoveries).</summary>
    public static Task<byte[]> ReadBytesAsync(HttpClient client,Uri uri,int maximumBytes,CancellationToken cancellation)=>
        ReceiveAsync(client,new HttpRequestMessage(HttpMethod.Get,uri),maximumBytes,cancellation,null);

    private static async Task<byte[]> ReceiveAsync(HttpClient client,HttpRequestMessage request,int maximumBytes,CancellationToken cancellation,TimeSpan? deadline)
    {
        using var message=request;
        if(maximumBytes<=0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        SentByHost.AddOrUpdate(message.RequestUri?.Host ?? "?",1,(_,count)=>count+1);
        message.Headers.UserAgent.ParseAdd(UserAgent);
        var limit=deadline ?? Deadline(client);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(limit);
        var token=timeout.Token;
        try
        {
            using var response=await client.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength>maximumBytes) throw new InvalidDataException("Community response too large.");
            await using var stream=await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var buffer=new MemoryStream(); var bytes=new byte[8192]; int read;
            while((read=await stream.ReadAsync(bytes,token).ConfigureAwait(false))>0)
            {
                if(buffer.Length+read>maximumBytes) throw new InvalidDataException("Community response too large.");
                buffer.Write(bytes,0,read);
            }
            token.ThrowIfCancellationRequested();
            return buffer.ToArray();
        }
        catch(OperationCanceledException error) when(!cancellation.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            throw new TaskCanceledException($"Request to {message.RequestUri?.Host} timed out after {limit.TotalSeconds:0} s (body included).",new TimeoutException(error.Message));
        }
    }
}
