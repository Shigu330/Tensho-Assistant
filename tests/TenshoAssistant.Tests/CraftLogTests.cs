using System.Numerics;
using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Character;
using TenshoAssistant.Craft;

internal static class CraftLogTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static RecipeDefinition Recipe()=>new(123,456,1,"Menuiserie",100,[],0,new(80,100,1000,100,100,100,100,0,0,0));
    private static CraftActionCatalog Catalog()=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,spec.Kind+" "+job,1,"Native"))),"fixture");
    private static CraftLogInput Input(ulong owner=1)=>new(owner,Recipe(),new(100,1000,1000,500),Catalog(),[],new(1000,4,32,5000),0,true);
    private static CraftLogResult Reply(string message)=>new(null,null,false,message);
    private static CharacterStatsState Stats(DateTimeOffset now)=>new(1,"Artisan",8,"Menuisier",100,now,
        new Dictionary<string,int>{{"Craftsmanship",1000},{"Control",1000},{"CraftingPoints",500}});
    private static CraftLogObservation Observation(DateTimeOffset now)=>new(1,123,456,0,new(100,200),new(500,600),now,8,StartingQuality:0,MaximumQuality:1000);
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Craft log observation rejects invalid geometry and consumes bounded recipe opening once",()=>
        {
            var now=DateTimeOffset.UtcNow;var observation=Observation(now);var service=new CraftLogObservationService();service.Publish(observation);
            Check(observation.Fresh(now) && !observation.Fresh(now.AddSeconds(2)) && !observation.Fresh(now.AddTicks(-1)),"freshness bounded in both directions");
            Throws<ArgumentException>(()=>service.Publish(observation with {Size=new(float.NaN,10)}));
            Throws<ArgumentException>(()=>service.Publish(observation with {CraftTypeId=8}));
            Throws<ArgumentException>(()=>service.RequestOpen(new(0,123,456)));
            Throws<ArgumentException>(()=>service.RequestOpen(new(1,65536,456)));
            service.RequestOpen(new(1,123,456));Check(service.TakeOpen()==new CraftLogOpenRequest(1,123,456) && service.TakeOpen() is null,"single consumption");
            service.RequestOpen(new(1,123,456));service.Reset();Check(service.TakeOpen() is null && service.Observation is null,"reset clears pending actions and observations");
            service.Publish(observation);Check(service.Observation!.Revision!=observation.Revision,"same owner after reopening belongs to a new observation revision");
        }),
        ("Craft log context binds full recipe identity current owner job freshness and observed attributes",()=>
        {
            var now=DateTimeOffset.UtcNow;var observation=Observation(now);var stats=Stats(now);var catalog=RecipeCatalog.Build([Recipe()],"fixture");
            var valid=CraftLogContext.Resolve(observation,stats,catalog,now);
            Check(valid.Issue is null && valid.Stats is {Craftsmanship:1000,ManipulationAllowed:false},"current job attributes accepted without inventing quest unlock");
            foreach(var wrong in new[]{observation with {RecipeId=124},observation with {ItemId=457},observation with {CraftTypeId=1},observation with {CurrentJobId=9},observation with {ObservedAt=now.AddSeconds(-2)}})
                Check(CraftLogContext.Resolve(wrong,stats,catalog,now).Stats is null,"foreign identity or stale selection rejected");
            foreach(var wrong in new[]{stats with {CharacterId=2},stats with {JobId=9},stats with {ObservedAt=now.AddSeconds(-4)},stats with {ObservedAt=now.AddSeconds(1)},stats with {Values=new Dictionary<string,int>()}})
                Check(CraftLogContext.Resolve(observation,wrong,catalog,now).Stats is null,"scope job and metadata guards");
            Check(CraftLogContext.Resolve(observation,stats,null,now).Stats is null,"catalog not ready is unknown");
        }),
        ("Craft log planner compares compatible saved macros and generates a complete copyable sequence",()=>
        {
            var recipe=Recipe();var low=CraftPresets.Create(recipe,"Faible",[CraftActionKind.Observe,CraftActionKind.BasicSynthesis]);
            var high=CraftPresets.Create(recipe,"Qualité",[CraftActionKind.BasicTouch,CraftActionKind.BasicSynthesis]);
            var foreign=high with {RecipeId=124};var incomplete=high with {Actions=Array.AsReadOnly(new[]{CraftActionKind.Observe})};
            var result=CraftLogSession.Build(Input() with {Presets=new[]{low,high,foreign,incomplete}});
            Check(result.Saved?.Name=="Qualité" && result.Saved.Simulation.Final.Quality>0,"only finished matching recipes compared using current stats");
            Check(result.Suggested is {Simulation.Final.Finish:CraftFinish.Completed} && result.Suggested.Export.Pages.All(page=>page.Lines.Count<=15),"solver and common exporter produce valid macro blocks");
            Check(result.Suggested!.Export.Actions==result.Suggested.Simulation.Steps.Count,"no omitted action in completion prefix");
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();Throws<OperationCanceledException>(()=>CraftLogSession.Build(Input(),cancelled.Token));
        }),
        ("Craft log late calculations cannot replace a new recipe or character",()=>
        {
            foreach(var mode in new[]{0,1,2})
            {
                using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
                using var session=new CraftLogSession((input,_)=>{if(input.ObservationRevision==0){entered.Set();release.Wait();return Reply("old");}return Reply("new");});
                var old=session.StartAsync(Input());Check(entered.Wait(TimeSpan.FromSeconds(5)),"old worker started");
                try
                {
                    var next=Input(mode==0 ? 2UL:1UL) with {Recipe=Recipe() with {RecipeId=mode==2 ? 123U:124U},ObservationRevision=1};
                    session.StartAsync(next).GetAwaiter().GetResult();var expected=session.State;
                    release.Set();old.GetAwaiter().GetResult();Check(ReferenceEquals(session.State,expected) && session.State.Result?.SearchIssue=="new","late ignored even when builder ignores cancellation");
                }
                finally{release.Set();old.GetAwaiter().GetResult();}
            }
        }),
        ("Craft log close disable and disposal cancel pending results without resurrection",()=>
        {
            foreach(var dispose in new[]{false,true})
            {
                using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
                using var session=new CraftLogSession((_,_)=>{entered.Set();release.Wait();return Reply("late");});
                var work=session.StartAsync(Input());Check(entered.Wait(TimeSpan.FromSeconds(5)),"worker started");
                try
                {
                    if(dispose)session.Dispose();else session.Cancel();var expected=session.State;release.Set();work.GetAwaiter().GetResult();
                    Check(ReferenceEquals(expected,session.State) && session.State.Input is null && session.State.Result is null,"closed context never republished");
                    if(dispose){session.StartAsync(Input()).GetAwaiter().GetResult();Check(ReferenceEquals(expected,session.State),"disposed inert");}
                }
                finally{release.Set();work.GetAwaiter().GetResult();}
            }
        }),
        ("Craft log worker owns recipe and saved action copies and reports failures",()=>
        {
            using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
            var actions=new List<CraftActionKind>{CraftActionKind.BasicSynthesis};var ingredients=new List<RecipeIngredient>{new(1,1)};
            var preset=CraftPresets.Create(Recipe(),"Copie",actions) with {Actions=actions};var presets=new List<CraftPreset>{preset};
            using var session=new CraftLogSession((input,_)=>{entered.Set();release.Wait();Check(input.Presets.Single().Actions.Count==1 && input.Recipe.Ingredients.Count==1,"producer mutations detached");return Reply("copied");});
            var task=session.StartAsync(Input() with {Presets=presets,Recipe=Recipe() with {Ingredients=ingredients}});
            try{Check(entered.Wait(TimeSpan.FromSeconds(5)),"worker started");actions.Clear();presets.Clear();ingredients.Clear();release.Set();task.GetAwaiter().GetResult();Check(session.State.Result?.SearchIssue=="copied","managed copies retained");}
            finally{release.Set();task.GetAwaiter().GetResult();}
            using var failed=new CraftLogSession((_,_)=>throw new InvalidOperationException("failure"));failed.StartAsync(Input()).GetAwaiter().GetResult();
            Check(failed.State.Error=="failure" && !failed.State.Loading && failed.State.Result is null,"failure explicit without previous result");
        }),
        ("Craft log preference defaults on for existing configs and remains independently persisted",()=>
        {
            var config=JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
            Check(config.CraftLogHelperEnabled && !config.MarketPricesOnHover,"requested passive helper never enables prices");
            config.CraftLogHelperEnabled=false;var copy=JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;
            Check(!copy.CraftLogHelperEnabled && copy.Version==1,"disabled helper survives reload without schema migration");
        }),
    };
}
