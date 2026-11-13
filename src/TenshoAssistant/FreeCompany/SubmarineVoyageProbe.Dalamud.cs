using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.FreeCompany;

/// <summary>
/// DEC-043 (0.1006): before the game's own submarine functions replace the community formula, a probe proves what they compute.
/// Developer mode only, once per session, logged in, on the framework thread, only when the game resolved the three functions
/// (HousingManager.GetSubmarineVoyageTime, GetSubmarineVoyageDistance, GetSubmarineSurveyDuration: static, they only compute; nothing is
/// sent to the server). For each map, every leg (start and sectors) and every survey is compared with VoyagePlanner's formula, with the
/// game's point taken as the sheet row and as the row minus 1 (FFXIVClientStructs: « each map uses 0 index as their starting point »).
/// The counts go to the log (« Submarine voyage check »); nothing is used yet. Removed on dispose.
/// 0.1007 (first reading, docs/evidence/0.1006-submarine-functions.md): outside the company workshop the three functions give 0 for every point and
/// speed; the probe now tries again after each zone change while it reads nothing but 0, and stops after the first reading with a value.
/// </summary>
public sealed unsafe class SubmarineVoyageProbe : IDisposable
{
    private static readonly int[] Speeds=[20,100,180];
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private readonly Func<bool> developer;
    private bool done;
    private uint triedIn=uint.MaxValue;

    public SubmarineVoyageProbe(IFramework framework,IClientState client,IDataManager data,IPluginLog log,Func<bool> developer)
    {
        this.framework=framework;this.client=client;this.data=data;this.log=log;this.developer=developer;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        if(done || !developer() || !client.IsLoggedIn || (uint)client.TerritoryType==triedIn)return;
        triedIn=(uint)client.TerritoryType;
        if((nint)HousingManager.MemberFunctionPointers.GetSubmarineVoyageTime==0 || (nint)HousingManager.MemberFunctionPointers.GetSubmarineVoyageDistance==0
            || (nint)HousingManager.MemberFunctionPointers.GetSubmarineSurveyDuration==0)
        {done=true;log.Info("Submarine voyage check: the game's functions are not resolved in this version.");return;}
        var anyValue=false;var lines=new List<string>();
        try
        {
            var sectors=data.GetExcelSheet<SubmarineExploration>().Where(row=>row.Map.RowId!=0)
                .Select(row=>new SubmarineSector(row.RowId,row.Map.RowId,row.StartingPoint,row.RankReq,new System.Numerics.Vector3(row.X,row.Y,row.Z),row.SurveyDistance,
                    row.SurveyDurationmin,row.ExpReward,"")).Where(sector=>sector.Id<=byte.MaxValue).ToArray();
            // Only points valid under both readings: as the row and as the row minus 1, the game may look up the next row (never past the sheet).
            var lastRow=sectors.Max(sector=>sector.Id);
            foreach(var offset in new[]{0,1})
                foreach(var speed in Speeds)
                {
                    int legs=0,timeSame=0,distanceSame=0,surveys=0,surveySame=0;var samples=new List<string>();
                    foreach(var map in sectors.GroupBy(sector=>sector.Map))
                    {
                        var points=map.OrderBy(sector=>sector.Id).ToArray();
                        foreach(var to in points.Where(sector=>!sector.Start && sector.Id<lastRow))
                        {
                            var b=(byte)(to.Id-offset);
                            var survey=HousingManager.GetSubmarineSurveyDuration(b,(short)speed);surveys++;
                            if(survey!=0)anyValue=true;
                            if(survey==VoyagePlanner.SurveySeconds(to,speed))surveySame++;
                            foreach(var from in points)
                            {
                                if(from.Id==to.Id || from.Id>=lastRow)continue;
                                var a=(byte)(from.Id-offset);
                                var time=HousingManager.GetSubmarineVoyageTime(a,b,(short)speed);
                                var distance=HousingManager.GetSubmarineVoyageDistance(a,b);
                                legs++;
                                var formulaTime=VoyagePlanner.LegSeconds(from,to,speed);var formulaDistance=VoyagePlanner.LegDistance(from,to);
                                if(time!=0 || distance!=0)anyValue=true;
                                if(time==formulaTime)timeSame++;
                                if(distance==formulaDistance)distanceSame++;
                                if(samples.Count<4 && (time!=formulaTime || distance!=formulaDistance))
                                    samples.Add($"{from.Id}->{to.Id} game {time}/{distance} formula {formulaTime}/{formulaDistance}");
                            }
                            if(samples.Count<6 && survey!=VoyagePlanner.SurveySeconds(to,speed))samples.Add($"survey {to.Id} game {survey} formula {VoyagePlanner.SurveySeconds(to,speed)}");
                        }
                    }
                    lines.Add($"Submarine voyage check (territory {triedIn}, point = row - {offset}, speed {speed}): {legs} legs, time equal {timeSame}, distance equal {distanceSame}; {surveys} surveys, equal {surveySame}; differences: {(samples.Count==0 ? "none":string.Join("; ",samples))}");
                }
            if(anyValue){done=true;Write(lines);}
            else log.Debug("Submarine voyage check (territory {0}): the game's functions give 0 here; tried again in the next zone.",triedIn);
        }
        catch(Exception error){done=true;log.Warning(error,"Submarine voyage check failed.");}
    }

    // X-177 C6 (0.1204): the same lines at each reload of the plugin: written whole once per game session (SessionOnce), then one short line.
    private void Write(List<string> lines)
    {
        if(TenshoAssistant.Diagnostics.SessionOnce.First(string.Join("\n",lines)))foreach(var line in lines)log.Info(line);
        else log.Info($"Submarine voyage check (territory {triedIn}): the same {lines.Count} lines as earlier in this game session.");
    }

    public void Dispose()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
}
