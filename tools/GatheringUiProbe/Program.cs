using Lumina;
using Lumina.Data.Files;
using TenshoAssistant.Gathering;

if(args.Length!=1)throw new ArgumentException("Supply installed sqpack directory.");
using var data=new GameData(args[0]);
var file=data.GetFile<UldFile>("ui/uld/Gathering.uld") ?? throw new Exception("Gathering ULD missing.");
Console.WriteLine($"Gathering ULD: {file.Components.Length} components, {file.WidgetData.Nodes.Length} widget nodes");
foreach(var component in file.Components)
    Console.WriteLine($"COMPONENT {component.Id} type={component.Type} nodes={component.Nodes.Length}; "+string.Join(",",component.Nodes.Where(node=>node.NodeId is 7 or 10 or 16 or 31).Select(node=>$"{node.NodeId}:type{node.NodeType}")));
var rows=file.Components.Where(component=>component.Nodes.Any(node=>node.NodeId==GatheringUiLayout.Success && node.NodeType==3)
    && component.Nodes.Any(node=>node.NodeId==GatheringUiLayout.Boon && node.NodeType==3) && component.Nodes.Any(node=>node.NodeId==GatheringUiLayout.Icon && node.NodeType>=1000)).ToArray();
if(rows.Length!=1)throw new Exception("Gathering row component identity ambiguous.");
var row=rows.Single();var iconType=row.Nodes.Single(node=>node.NodeId==GatheringUiLayout.Icon).NodeType;
var icon=file.Components.Single(component=>component.Id==iconType);
if(!icon.Nodes.Any(node=>node.NodeId==GatheringUiLayout.Quantity && node.NodeType==3) || row.Nodes.Length>GatheringUiLayout.MaxNodes || icon.Nodes.Length>GatheringUiLayout.MaxNodes)
    throw new Exception("Gathering quantity node or reader bounds incompatible.");
var instances=file.WidgetData.Nodes.Count(node=>node.NodeType==row.Id);
if(instances!=8)throw new Exception($"Expected eight item row instances; found {instances}.");
Console.WriteLine($"PASS installed ULD: eight rows component={row.Id} ({row.Type}), success text10, boon text16, icon31->component{icon.Id} ({icon.Type}), quantity text7; node counts within256.");
