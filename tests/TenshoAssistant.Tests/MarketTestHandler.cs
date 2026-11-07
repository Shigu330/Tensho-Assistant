using System.Net;
using System.Net.Http;

internal sealed class MarketTestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        request.RequestUri?.Query.Contains("listings=0",StringComparison.Ordinal)==true
            ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")})
            : send(request, cancellationToken);
}
