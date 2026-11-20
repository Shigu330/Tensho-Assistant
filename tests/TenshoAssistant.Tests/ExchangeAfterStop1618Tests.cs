using System.Net.Http;
using TenshoAssistant.Discoveries;

// 0.1618 (found by a red run of GitHubToken1171 N8 under load, after 0.1614 X-666): the count of failed exchanges was saved after the plugin
// had asked the exchange to stop (unload): github.json written while its folder was being removed. Nothing is written once the stop is asked.
internal static class ExchangeAfterStop1618Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,16,0,0,TimeSpan.Zero);

    private sealed class Held : HttpMessageHandler
    {
        public readonly TaskCompletionSource Asked=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Go=new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Asked.TrySetResult();
            await Go.Task.ConfigureAwait(false);
            throw new InvalidOperationException("failed after the unload");
        }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Exchange after stop 1618: behaviour: an exchange that ends after the unload writes nothing",()=>
        {
            var root=Path.Combine(Path.GetTempPath(),"tensho-afterstop-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var held=new Held();
                var exchange=new GitHubExchange(root,held,GitHubSignIn.For("Iv1.test"),()=>At);
                Check(exchange.Exchange(new DiscoveryStore(),false) && held.Asked.Task.Wait(TimeSpan.FromSeconds(10)),"the exchange asks GitHub");
                exchange.Dispose();
                held.Go.SetResult();
                for(var wait=0;exchange.Busy && wait<1500;wait++)Thread.Sleep(10);
                Check(!exchange.Busy,"the exchange ends");
                Check(!File.Exists(Path.Combine(root,"github.json")),"nothing written after the unload");
                var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs"));
                Check(code.Contains("if(exchange && result.Key!=\"ExchangeStopped\" && !stop.IsCancellationRequested)"),"the count skipped once the stop is asked");
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}}
        }),
    ];
}
