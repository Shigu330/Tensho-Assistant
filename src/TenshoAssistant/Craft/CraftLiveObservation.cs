using System.Globalization;

namespace TenshoAssistant.Craft;

public enum CraftLiveStatusKind {Disabled,Waiting,Offline,AddonAbsent,NotReady,Observed,Error}
public sealed record CraftLiveEffect(string Name,string Turns);
public sealed record CraftLiveStatus(uint Id,ushort Param,float RemainingTime);
public sealed record CraftLiveNumbers(string Progress,string MaxProgress,string Quality,string MaxQuality,string Durability,string MaxDurability,string Step);
public sealed record CraftLiveRaw(ushort RecipeId,int ResultItemId,int CraftType,ushort Step,byte Condition,byte Flags,uint StateFlags,
    uint JobId,CraftStats Stats,uint Cp,uint MaxCp,string ItemName,string ConditionText,string EffectsOverflow,CraftLiveNumbers Numbers,
    IReadOnlyList<CraftLiveEffect> Effects,IReadOnlyList<CraftLiveStatus> Statuses,bool StatusesComplete=false,
    CraftNativeNumbers? NumbersNative=null,IReadOnlyList<CraftNativeAction>? ActionsNative=null,bool ActionsComplete=false,CraftLiveTool? Tool=null);
public sealed class CraftLiveFrame
{
    public CraftLiveRaw Raw {get;}
    public RecipeDefinition? Recipe {get;}
    public bool Animating=>(Raw.Flags&9)!=0;
    public int? Progress {get;}
    public int? Quality {get;}
    public int? Durability {get;}
    public int? UiStep {get;}
    public string Source=>"FFXIVClientStructs.CraftEventHandler + AddonSynthesis + Dalamud.LocalPlayer";
    public CraftLiveFrame(CraftLiveRaw raw,RecipeCatalog? catalog)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if(raw.Effects.Count>9 || raw.Statuses.Count>60 || raw.Cp>raw.MaxCp || raw.MaxCp is 0 or >10000 || raw.JobId is <8 or >15
            || raw.Stats.Level is <1 or >100 || raw.Stats.Craftsmanship<0 || raw.Stats.Control<0 || raw.ActionsNative?.Count>CraftActionCatalog.Specs.Count)
            throw new ArgumentException("Observation de synthèse invalide.");
        static string Bounded(string value)=>value.Length<=512 ? value:throw new ArgumentException("Texte natif trop long.");
        var n=raw.Numbers;
        Raw=raw with {ItemName=Bounded(raw.ItemName),ConditionText=Bounded(raw.ConditionText),EffectsOverflow=Bounded(raw.EffectsOverflow),
            Numbers=new(Bounded(n.Progress),Bounded(n.MaxProgress),Bounded(n.Quality),Bounded(n.MaxQuality),Bounded(n.Durability),Bounded(n.MaxDurability),Bounded(n.Step)),
            Effects=Array.AsReadOnly(raw.Effects.Select(effect=>new CraftLiveEffect(Bounded(effect.Name),Bounded(effect.Turns))).ToArray()),
            Statuses=Array.AsReadOnly(raw.Statuses.ToArray()),ActionsNative=raw.ActionsNative is null ? null:Array.AsReadOnly(raw.ActionsNative.ToArray())};
        if(raw.ResultItemId>0 && raw.RecipeId>0 && raw.CraftType is >=0 and <=7 && raw.JobId==(uint)raw.CraftType+8)
            Recipe=catalog?.ForResult((uint)raw.ResultItemId).FirstOrDefault(recipe=>recipe.RecipeId==raw.RecipeId && recipe.CraftTypeId==(uint)raw.CraftType);
        Progress=Within(ParseNumber(n.Progress),ParseNumber(n.MaxProgress));
        Quality=Within(ParseNumber(n.Quality),ParseNumber(n.MaxQuality));
        Durability=Within(ParseNumber(n.Durability),ParseNumber(n.MaxDurability));
        UiStep=ParseNumber(n.Step);
    }
    private static int? Within(int? value,int? maximum)=>value is not null && maximum is not null && value<=maximum ? value:null;
    public static int? ParseNumber(string text)
    {
        text=text.Trim(); if(text.Length is 0 or >32)return null;
        text=new string(text.Select(character=>character is '\u00a0' or '\u202f' or '\u2009' ? ' ':character).ToArray());
        var separators=text.Where(character=>!char.IsAsciiDigit(character)).Distinct().ToArray();
        if(separators.Length>0)
        {
            if(separators.Length!=1 || separators[0] is not (' ' or ',' or '.'))return null;
            var groups=text.Split(separators[0]);
            if(groups[0].Length is <1 or >3 || groups.Skip(1).Any(group=>group.Length!=3))return null;
            text=string.Concat(groups);
        }
        return text.All(char.IsAsciiDigit) && int.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out var value) ? value:null;
    }
}
public sealed record CraftLiveState(bool Enabled,ulong Owner,CraftLiveStatusKind Status,DateTimeOffset? ObservedAt,CraftLiveFrame? Frame,string? Error);
public sealed class CraftLiveService : IDisposable
{
    private readonly object gate=new(); private int generation; private bool disposed;
    private CraftLiveState state=new(false,0,CraftLiveStatusKind.Disabled,null,null,null);
    public CraftLiveState State=>Volatile.Read(ref state);
    public void SetEnabled(bool enabled)
    {lock(gate){if(disposed || state.Enabled==enabled)return;generation++;state=new(enabled,0,enabled ? CraftLiveStatusKind.Waiting:CraftLiveStatusKind.Disabled,null,null,null);}}
    internal int? BeginRead(ulong owner)
    {
        lock(gate)
        {
            if(disposed || !state.Enabled)return null;
            if(state.Owner!=owner || owner==0 && state.Status!=CraftLiveStatusKind.Offline)
            {generation++;state=new(true,owner,owner==0 ? CraftLiveStatusKind.Offline:CraftLiveStatusKind.Waiting,null,null,null);}
            return owner==0 ? null:generation;
        }
    }
    private bool Accept(int ticket,ulong owner)=>!disposed && state.Enabled && owner!=0 && state.Owner==owner && ticket==generation;
    internal void Publish(int ticket,ulong owner,CraftLiveRaw raw,RecipeCatalog? catalog)
    {
        lock(gate)
        {
            if(!Accept(ticket,owner))return;
            try {state=new(true,owner,CraftLiveStatusKind.Observed,DateTimeOffset.UtcNow,new(raw,catalog),null);}
            catch(ArgumentException error){state=new(true,owner,CraftLiveStatusKind.Error,null,null,error.Message);throw;}
        }
    }
    internal void Missing(int ticket,ulong owner,CraftLiveStatusKind reason)
    {
        if(reason is not (CraftLiveStatusKind.AddonAbsent or CraftLiveStatusKind.NotReady))throw new ArgumentOutOfRangeException(nameof(reason));
        lock(gate)if(Accept(ticket,owner))state=new(true,owner,reason,null,null,null);
    }
    internal void Fail(int ticket,ulong owner,Exception error)
    {lock(gate)if(Accept(ticket,owner))state=new(true,owner,CraftLiveStatusKind.Error,null,null,error.Message);}
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;generation++;state=new(false,0,CraftLiveStatusKind.Disabled,null,null,null);}}
}
