using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;

namespace TenshoAssistant.Fields;

/// <summary>
/// X-188 (0.1176): the Occult Crescent's state (PublicContentOccultCrescent.State, a field of the island's director: no game function called), read once
/// a second on the framework thread while that director exists, copied into <see cref="OccultState"/>; the last one is kept for the session (the page
/// shows when it was read). A reading that fails is said once and tried again a minute later (R08).
/// </summary>
public sealed unsafe class OccultCrescentReader : IDisposable
{
    private readonly IFramework framework;private readonly IPluginLog log;
    private OccultState? state;private long nextRead=-100_000;private bool disposed,warned;
    public OccultState? State=>Volatile.Read(ref state);

    public OccultCrescentReader(IFramework framework,IPluginLog log)
    {
        this.framework=framework;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);
    }

    private void Update(IFramework unused)
    {
        var now=Environment.TickCount64;
        if(disposed || now<nextRead)return;
        nextRead=now+1_000;
        try
        {
            var director=EventFramework.GetPublicContentDirectorByType(PublicContentDirectorType.OccultCrescent);
            if(director==null)return;
            var held=((PublicContentOccultCrescent*)director)->State;
            var levels=new int[held.SupportJobLevels.Length];
            for(var index=0;index<levels.Length;index++)levels[index]=held.SupportJobLevels[index];
            var read=new OccultState(DateTimeOffset.UtcNow,held.CurrentKnowledge,held.NeededKnowledge,held.CurrentSupportJob,levels,held.SupportJobExperience.ToArray(),
                held.NeededJobExperience,held.Silver,held.Gold);
            if(OccultCrescent.Empty(read))return;
            Volatile.Write(ref state,read);
            warned=false;
        }
        catch(Exception error)
        {
            nextRead=now+60_000;
            if(!warned){warned=true;log.Warning(error,"The Occult Crescent state could not be read (perhaps after a game update).");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);
    }
}
