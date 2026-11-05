using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>
/// 0.525 (user report: party members jump on the map): between two positions the marker glides. A member out of view only gets a position from the game now and then, so reading faster is not enough.
/// A long jump (a teleport) is not glided over.
/// </summary>
public sealed class MapPartySmoother
{
    public const float SnapDistance=60,Rate=10;
    private const int MaxMembers=32;
    private readonly Dictionary<string,Vector3> shown=[];

    public Vector3 At(string member,Vector3 target,float seconds)
    {
        if(!shown.TryGetValue(member,out var at) || Vector3.Distance(at,target)>SnapDistance)
        {
            if(shown.Count>=MaxMembers)shown.Clear();
            shown[member]=target;
            return target;
        }
        var next=Vector3.Lerp(at,target,1-MathF.Exp(-Math.Max(0,seconds)*Rate));
        shown[member]=next;
        return next;
    }
}
