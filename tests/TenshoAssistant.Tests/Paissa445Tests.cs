using TenshoAssistant.Community;

// N-05 (0.445): PaissaDB open plots — parsed (0-based ids shown from 1), new plots after a first read, watch off by default.
internal static class Paissa445Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private const string World="""
        {"id":40,"name":"Moogle","num_open_plots":3,"districts":[
          {"id":339,"name":"Mist","num_open_plots":2,"open_plots":[
            {"world_id":40,"district_id":339,"ward_number":4,"plot_number":11,"size":1,"price":3000000,"last_updated_time":1.7e9,"first_seen_time":1.7e9,
             "est_time_open_min":0,"est_time_open_max":0,"purchase_system":5,"lotto_entries":12,"lotto_phase":1,"lotto_phase_until":1700100000},
            {"world_id":40,"district_id":339,"ward_number":0,"plot_number":0,"size":0,"price":3000000,"purchase_system":2,"lotto_entries":null,"lotto_phase":null},
            {"district_id":339,"ward_number":"bad"}]},
          {"id":641,"name":"Shirogane","open_plots":[{"world_id":40,"district_id":641,"ward_number":29,"plot_number":59,"size":2,"price":40000000,"purchase_system":7}]}]}
        """;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("PaissaDB: plots parsed 1-based, purchase bits, malformed plot skipped; new plots only after a first read",() =>
        {
            var plots=PaissaDb.Parse(World);
            Check(plots.Count==3,"three valid plots");
            Check(plots[0] is { District:339, Ward:1, Plot:1, Size:0, Lottery:false, FreeCompany:true, Individual:false, LottoEntries:null },"0-based → 1-based, FCFS free company");
            Check(plots[1] is { Ward:5, Plot:12, Size:1, Lottery:true, Individual:true, LottoEntries:12, LottoPhase:1 } && plots[1].PhaseUntil is not null,"lottery");
            Check(plots[2] is { District:641, Ward:30, Plot:60, Size:2, Price:40000000 },"large");
            Check(PaissaDb.New(null,plots).Count==0 && PaissaDb.New([plots[0]],plots).Count==2,"new only after a first read");
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Plots.cs"));
            Check(ui.Contains("Math.Clamp(configuration.PlotWatchMinutes,10,240)") && File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Configuration.cs")).Contains("public bool PlotWatch { get; set; }\n")
                ,"watch off by default, interval bounded");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Community/PaissaDb.cs")).Contains("$\"https://{PaissaDb.Host}/worlds/{worldId}\""),"only the world id is sent");
        }),
    ];
}
