using System.Net.Http;
using System.Text;

namespace TenshoAssistant.Community;

public static class BoundedHttp
{
    public static async Task<string> ReadAsync(HttpClient client,Uri uri,int maximumBytes,CancellationToken cancellation)
    {
        if(maximumBytes<=0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        using var message=new HttpRequestMessage(HttpMethod.Get,uri);
        message.Headers.UserAgent.ParseAdd($"TenshoAssistant/{typeof(BoundedHttp).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}");
        using var response=await client.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,cancellation).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>maximumBytes) throw new InvalidDataException("Réponse communautaire trop volumineuse.");
        await using var stream=await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
        using var buffer=new MemoryStream(); var bytes=new byte[8192]; int read;
        while((read=await stream.ReadAsync(bytes,cancellation).ConfigureAwait(false))>0)
        {
            if(buffer.Length+read>maximumBytes) throw new InvalidDataException("Réponse communautaire trop volumineuse.");
            buffer.Write(bytes,0,read);
        }
        cancellation.ThrowIfCancellationRequested();
        return new UTF8Encoding(false,true).GetString(buffer.GetBuffer(),0,(int)buffer.Length);
    }
}
