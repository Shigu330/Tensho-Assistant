using System.Globalization;

namespace TenshoAssistant.Craft;

public sealed record CraftMacroPage(int Number,IReadOnlyList<string> Lines)
{
    public string Text=>string.Join('\n',Lines);
}
public sealed record CraftMacroExport(IReadOnlyList<CraftMacroPage> Pages,int Actions,int WaitSeconds,int OmittedActions);

public static class CraftMacroExporter
{
    public static int BaseWait(CraftActionKind action)=>action switch
    {
        CraftActionKind.Veneration or CraftActionKind.Innovation or CraftActionKind.GreatStrides
            or CraftActionKind.WasteNot or CraftActionKind.WasteNot2 or CraftActionKind.Manipulation=>2,
        _ when Enum.IsDefined(action)=>3,
        _=>throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static CraftMacroExport Build(CraftSimulationResult simulation,uint job,CraftActionCatalog catalog,int extraWait=0,bool echo=true)
    {
        ArgumentNullException.ThrowIfNull(simulation); ArgumentNullException.ThrowIfNull(catalog);
        if(extraWait is <0 or >3 || job is <8 or >15) throw new ArgumentOutOfRangeException(nameof(extraWait));
        if(simulation.Final.Finish!=CraftFinish.Completed || simulation.Error is not null || simulation.InvalidAction is not null || simulation.Steps.Count is <1 or >100)
            throw new ArgumentException("Terminez une simulation valide avant l'export.",nameof(simulation));
        var lines=new List<string>(); var seconds=0;
        foreach(var step in simulation.Steps)
        {
            var action=catalog.Find(job,step.Action) ?? throw new ArgumentException("Métadonnées de l'action indisponibles.",nameof(catalog));
            var name=action.Name;
            if(string.IsNullOrWhiteSpace(name) || name.Length>128 || name.Any(character=>char.IsControl(character) || character is '"' or '\u2028' or '\u2029'))
                throw new ArgumentException("Nom natif incompatible avec une ligne de macro.",nameof(catalog));
            var wait=BaseWait(step.Action)+extraWait;
            lines.Add($"/ac \"{name}\" <wait.{wait.ToString(CultureInfo.InvariantCulture)}>"); seconds+=wait;
        }
        var size=echo ? 14:15; var chunks=lines.Chunk(size).ToArray(); var pages=new List<CraftMacroPage>();
        for(var index=0;index<chunks.Length;index++)
        {
            var page=chunks[index].ToList();
            if(echo) page.Add($"/echo Tensho {index+1}/{chunks.Length} <se.1>");
            pages.Add(new(index+1,Array.AsReadOnly(page.ToArray())));
        }
        return new(Array.AsReadOnly(pages.ToArray()),lines.Count,seconds,Math.Max(0,simulation.PlannedActions-simulation.Steps.Count));
    }
}
