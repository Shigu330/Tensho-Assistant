using System.Net;
using TenshoAssistant.Community;
using TenshoAssistant.Desktop;

// 0.1123 (X-143 part 1, audit A06): a reply whose headers came but whose body stops coming no longer hangs: the community requests
// (BoundedHttp, used by the community help and the GitHub exchange among others) and the desktop app's download have a deadline that
// covers the body. Behaviour tests (R28): a fake server that sends its headers then nothing, with no cancellation from the caller.
internal static class HttpDeadline1123Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    /// <summary>A body that gives a few bytes, then waits for ever (until its read is cancelled).</summary>
    private sealed class StalledStream : Stream
    {
        private bool started;
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override void Flush(){}
        public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException("async only");
        public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
        public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default)
        {
            if(!started){started=true;buffer.Span[0]=(byte)'{';return 1;}
            await Task.Delay(Timeout.Infinite,token).ConfigureAwait(false);
            return 0;
        }
    }

    private sealed class StalledHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StreamContent(new StalledStream())});
    }

    private static (Exception? Error,long Ms) Run(Func<Task> work)
    {
        var watch=System.Diagnostics.Stopwatch.StartNew();
        try{work().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();return (null,watch.ElapsedMilliseconds);}
        catch(Exception error){return (error,watch.ElapsedMilliseconds);}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("HTTP 1123: a community reply that stalls after its headers ends at the deadline (the client's timeout), not never",() =>
        {
            using var client=new HttpClient(new StalledHandler()){Timeout=TimeSpan.FromMilliseconds(400)};
            var (error,ms)=Run(()=>BoundedHttp.ReadAsync(client,new Uri("https://example.invalid/stall"),1024,CancellationToken.None));
            Check(error is TaskCanceledException { InnerException: TimeoutException } && ms<10_000,$"timed out in {ms} ms: {error?.GetType().Name} {error?.Message}");
            Check(BoundedHttp.Deadline(client)==TimeSpan.FromMilliseconds(400) && BoundedHttp.Deadline(new HttpClient{Timeout=Timeout.InfiniteTimeSpan})==BoundedHttp.DefaultDeadline,"deadline: the client's when shorter, else 2 min");
            using var endless=new HttpClient(new StalledHandler()){Timeout=Timeout.InfiniteTimeSpan};
            (error,ms)=Run(()=>BoundedHttp.SendAsync(endless,new HttpRequestMessage(HttpMethod.Get,"https://example.invalid/stall"),1024,CancellationToken.None,TimeSpan.FromMilliseconds(300)));
            Check(error is TaskCanceledException { InnerException: TimeoutException } && ms<10_000,"a client without timeout still ends: "+ms+" ms");
        }),
        ("HTTP 1123: the caller's own cancellation stays its own (not a timeout)",() =>
        {
            using var client=new HttpClient(new StalledHandler()){Timeout=Timeout.InfiniteTimeSpan};
            using var stop=new CancellationTokenSource(300);
            var (error,_)=Run(()=>BoundedHttp.ReadAsync(client,new Uri("https://example.invalid/stall"),1024,stop.Token));
            Check(error is OperationCanceledException && error.InnerException is not TimeoutException,"cancelled by the caller: "+error?.GetType().Name);
        }),
        ("HTTP 1123: the desktop app's download that stalls ends at its deadline, the partial zip goes with it",() =>
        {
            var releases=new GitHubReleases(new HttpClient(new StalledHandler()),TimeSpan.FromMilliseconds(400));
            using var target=new MemoryStream();
            var asset=new ReleaseAsset(DesktopUpdate.ZipName,DesktopUpdate.DownloadPrefix+"Stable-0.1123.0/"+DesktopUpdate.ZipName,10);
            var (error,ms)=Run(()=>releases.Download(asset,target,_=>{},CancellationToken.None));
            Check(error is TaskCanceledException { InnerException: TimeoutException } && ms<10_000,$"the stalled download ends in {ms} ms: {error?.GetType().Name}");
        }),
    ];
}
