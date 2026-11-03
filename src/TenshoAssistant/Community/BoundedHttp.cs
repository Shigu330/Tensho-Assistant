using System.Net.Http;
using System.Text;

namespace TenshoAssistant.Community;

/// <summary>
/// Community requests read with a size limit: headers first, the declared length checked, then the body read by blocks and stopped as
/// soon as it passes the limit (a large reply is never loaded whole). Every request carries Tensho's User-Agent.
/// </summary>
public static class BoundedHttp
{
    public static Task<string> ReadAsync(HttpClient client,Uri uri,int maximumBytes,CancellationToken cancellation)=>
        SendAsync(client,new HttpRequestMessage(HttpMethod.Get,uri),maximumBytes,cancellation);

    /// <summary>Sends any request (0.321: POST for Teamcraft's GraphQL) and reads its reply within <paramref name="maximumBytes"/>.</summary>
    public static async Task<string> SendAsync(HttpClient client,HttpRequestMessage request,int maximumBytes,CancellationToken cancellation)
    {
        using var message=request;
        if(maximumBytes<=0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        message.Headers.UserAgent.ParseAdd($"TenshoAssistant/{typeof(BoundedHttp).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}");
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
