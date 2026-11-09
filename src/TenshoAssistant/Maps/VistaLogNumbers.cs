namespace TenshoAssistant.Maps;

/// <summary>
/// X-76 P5 (0.929): the sightseeing log number of a vista from the game's data. A Realm Reborn: the first 80 Adventure rows (from 2162688). Later
/// expansions: AdventureExPhase gives each phase's first and last Adventure row and its expansion (ExVersion); the number restarts with each
/// expansion and goes on across two phases of the same expansion (Dawntrail has two). Rows read with tools/XbmProbe on 2026-11-08:
/// 2162830-2162891 (2), 2162892-2162936 (3), 2162937-2162982 (4), 2162983-2163010 and 2163011-2163027 (5).
/// </summary>
public static class VistaLogNumbers
{
    public const uint FirstAdventure=2162688;
    public const int RealmRebornCount=80;

    public static (int Expansion,int Number)? Of(uint adventureId,IReadOnlyList<(uint Begin,uint End,int Expansion)> phases)
    {
        if(adventureId>=FirstAdventure && adventureId<FirstAdventure+RealmRebornCount)return (0,(int)(adventureId-FirstAdventure)+1);
        var before=new Dictionary<int,int>();
        foreach(var (begin,end,expansion) in phases.Where(phase=>phase.End>=phase.Begin).OrderBy(phase=>phase.Begin))
        {
            if(adventureId>=begin && adventureId<=end)return (expansion,before.GetValueOrDefault(expansion)+(int)(adventureId-begin)+1);
            before[expansion]=before.GetValueOrDefault(expansion)+(int)(end-begin)+1;
        }
        return null;
    }
}
