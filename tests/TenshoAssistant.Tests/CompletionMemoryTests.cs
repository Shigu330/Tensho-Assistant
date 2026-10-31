using TenshoAssistant.Collections;

internal static class CompletionMemoryTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly CollectionEntry Unknown = new(CollectionKind.Title,7,"Titre",0,false);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Titles and achievements keep obtained states and keep not-obtained only while fresh",()=>
        {
            Check(CompletionMemory.Merge(Unknown,(true,false),false).Acquisition == CollectionAcquisition.Acquired,"obtained stays obtained without a loaded list");
            Check(CompletionMemory.Merge(Unknown,(false,true),true).Acquisition == CollectionAcquisition.NotAcquired,"fresh not-obtained kept");
            Check(CompletionMemory.Merge(Unknown,(false,true),false).Acquisition == CollectionAcquisition.Unknown,"stale not-obtained becomes unknown");
            Check(CompletionMemory.Merge(Unknown with { ConfirmedUnlocked = true },(false,true),true).Acquisition == CollectionAcquisition.Acquired,"a new unlock wins over memory");
            Check(CompletionMemory.Merge(Unknown,null,true) == Unknown,"nothing remembered");
            Check(new TenshoAssistant.Caching.CachePreferences().Get(TenshoAssistant.Caching.CacheArea.Completion) == TenshoAssistant.Caching.CacheRetention.SevenDays,"seven days by default");
        }),
        ("Retainers and saddlebags are saved automatically unless turned off, older configurations included",()=>
        {
            Check(new TenshoAssistant.Configuration().AutoBankCapture,"on by default");
            Check(System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.Configuration>("{\"Version\":1}")!.AutoBankCapture,"older configuration: on");
            var off=new TenshoAssistant.Configuration{AutoBankCapture=false};
            Check(!System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.Configuration>(System.Text.Json.JsonSerializer.Serialize(off))!.AutoBankCapture,"choice kept");
        }),
    ];
}
