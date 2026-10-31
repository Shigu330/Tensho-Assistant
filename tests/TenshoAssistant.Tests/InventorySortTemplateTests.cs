using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Inventory;

internal static class InventorySortTemplateTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static readonly InventorySortScope Scope=InventorySortScope.Personal(7);
    private static InventorySortProfile Profile()=>InventorySortRules.Default(Scope) with{Zones=new[]{new InventorySortZone("Keep",0,3,Preserve:true),new InventorySortZone("Fruit",10,5,1,ReverseFill:true)}};
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Reusable sort templates own rule lists and contain no character storage or observed item identity",()=>
        {
            var criteria=new List<InventorySortCriterion>{new(InventorySortField.Name)};var zones=new List<InventorySortZone>{new("Fruit",10,5,1)};
            var template=InventorySortTemplates.Create(new(7,null,0,criteria,zones),Scope,"  Shared fruit  ");
            criteria.Clear();zones.Clear();Check(template.Name=="Shared fruit" && template.Criteria.Count==1 && template.Zones.Count==1,"trimmed bounded name and owned copies");
            var json=JsonSerializer.Serialize(template);
            Check(!json.Contains("CharacterId") && !json.Contains("StorageId") && !json.Contains("Snapshot") && !json.Contains("ObservedAt"),"portable rule schema has no runtime or owner evidence");
            Check(template.ContainerCount==4 && template.SlotsPerContainer==35 && template.Version==1 && template.Id!=Guid.Empty,"explicit geometry and version");
        }),
        ("Templates rebind only to compatible geometry across characters retainers and saddle groups",()=>
        {
            var template=InventorySortTemplates.Create(Profile(),Scope,"Personal");
            var other=InventorySortScope.Personal(8);var rebound=InventorySortTemplates.Load(template,other);
            Check(rebound.CharacterId==8 && rebound.StorageId==0 && rebound.Kind is null && rebound.Zones.SequenceEqual(Profile().Zones),"new owner with same geometry");
            var saddle=InventorySortScope.FromStorage(new(7,BankStorageKind.Saddlebag,0));var premium=InventorySortScope.FromStorage(new(8,BankStorageKind.PremiumSaddlebag,0));
            var shared=InventorySortTemplates.Create(InventorySortRules.Default(saddle),saddle,"Saddle");
            Check(InventorySortTemplates.Compatible(shared,premium) && InventorySortTemplates.Load(shared,premium).Kind==BankStorageKind.PremiumSaddlebag,"normal and premium share 2 x 35 layout only");
            var retainer=InventorySortScope.FromStorage(new(7,BankStorageKind.Retainer,91));var another=InventorySortScope.FromStorage(new(8,BankStorageKind.Retainer,92));
            var retainerTemplate=InventorySortTemplates.Create(InventorySortRules.Default(retainer),retainer,"Retainer");
            Check(InventorySortTemplates.Load(retainerTemplate,another).StorageId==92,"never retains original retainer ID");
            Refuses(()=>InventorySortTemplates.Load(template,retainer));Refuses(()=>InventorySortTemplates.Load(shared,Scope));
        }),
        ("Invalid template versions geometry bounds and overlapping rules refuse loading",()=>
        {
            var template=InventorySortTemplates.Create(Profile(),Scope,"Valid");
            foreach(var invalid in new[]{template with{Version=2},template with{Id=Guid.Empty},template with{Name=""},template with{Name=new string('x',81)},template with{ContainerCount=8},template with{SlotsPerContainer=25},
                template with{Zones=new[]{new InventorySortZone("Out",139,2)}},template with{Zones=new[]{new InventorySortZone("A",0,2),new InventorySortZone("B",1,2)}}})
            {Refuses(()=>InventorySortTemplates.Load(invalid,Scope));Check(!InventorySortTemplates.Compatible(invalid,Scope),"invalid template not advertised as compatible");}
        }),
        ("Template library upsert delete duplicate name and capacity preserve unrelated entries",()=>
        {
            var first=InventorySortTemplates.Create(Profile(),Scope,"First");var second=InventorySortTemplates.Create(Profile(),Scope,"Second");
            var saved=InventorySortTemplates.Save(new[]{first},second);
            var edited=InventorySortTemplates.Create(InventorySortRules.Default(Scope),Scope,"Renamed",first.Id);
            var updated=InventorySortTemplates.Save(saved,edited);
            Check(updated.Count==2 && updated.Single(t=>t.Id==second.Id).Zones.SequenceEqual(second.Zones) && updated.Single(t=>t.Id==first.Id).Zones.Count==0,"upsert changes exact ID, not another name");
            Refuses(()=>InventorySortTemplates.Save(updated,InventorySortTemplates.Create(Profile(),Scope,"SECOND")));
            var saddle=InventorySortScope.FromStorage(new(7,BankStorageKind.Saddlebag,0));
            Refuses(()=>InventorySortTemplates.Save(updated,InventorySortTemplates.Create(InventorySortRules.Default(saddle),saddle,"Changed geometry",first.Id)));
            var full=Enumerable.Range(0,64).Select(i=>InventorySortTemplates.Create(Profile(),Scope,"Template "+i)).ToArray();
            Refuses(()=>InventorySortTemplates.Save(full,first));
            Check(InventorySortTemplates.Save(full,full[0] with{Name="Updated"}).Count==64,"update permitted at capacity");
            var deleted=InventorySortTemplates.Delete(updated,first.Id);Check(deleted.Count==1 && deleted[0].Id==second.Id && deleted[0].Zones.SequenceEqual(second.Zones) && updated.Count==2,"deletion is a new list and preserves other template");
        }),
        ("Template library survives v1 serialization without replacing character profiles or creating defaults",()=>
        {
            var template=InventorySortTemplates.Create(Profile(),Scope,"Shared");
            var configuration=new Configuration{InventorySortTemplates=[template],InventorySortProfiles=[Profile()]};
            var copy=JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(configuration))!;
            Check(copy.Version==1 && copy.InventorySortProfiles?.Single().CharacterId==7,"profile ownership separate from shared library");
            var restored=copy.InventorySortTemplates!.Single();InventorySortTemplates.Validate(restored);
            Check(restored.Id==template.Id && restored.Zones.SequenceEqual(template.Zones) && restored.Criteria.SequenceEqual(template.Criteria),"immutable nested rules roundtrip");
            var old=JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
            Check(old.InventorySortTemplates?.Count==0 && old.InventorySortProfiles?.Count==0,"legacy config adds no implicit models or profiles");
        }),
        ("Loaded template reaches positioned preview in a new saddle scope without authorizing a native write",()=>
        {
            var source=InventorySortScope.FromStorage(new(7,BankStorageKind.Saddlebag,0));var target=InventorySortScope.FromStorage(new(8,BankStorageKind.PremiumSaddlebag,0));
            var profile=InventorySortRules.Default(source) with{Zones=new[]{new InventorySortZone("Reverse",35,5,1,ReverseFill:true)}};
            var template=InventorySortTemplates.Create(profile,source,"Shared saddle");var loaded=InventorySortTemplates.Load(template,target);var now=DateTimeOffset.UtcNow;
            var snapshot=new InventorySnapshot(now,"fixture",Enumerable.Range(0,2).Select(page=>new InventoryContainerSnapshot(new(new(8),new(4100+page)),InventoryCoverage.Complete,
                page==0 ? new InventoryStack[]{new(new(new(42),false),3,0,0),new(new(new(43),true),4,2,2)}:[])));
            var result=InventorySortRules.Build(snapshot.Containers.Values,target,loaded,id=>("Item "+id,1U));var plan=InventoryVisualSortPlan.Build(snapshot,result,target);
            Check(result.Slots![39]?.Stack.Item.Id.Value==42 && result.Slots[38]?.Stack.Item.Id.Value==43 && plan.Order[39]==new InventoryPhysicalSlot(4100,0),"reused geometry, new native coordinates and correct reverse targets");
            using var service=new InventoryVisualSortService();
            Check(!service.State.Pending && !service.Request(plan,now),"loading a template cannot replace required direct capture receipt");
            Check(loaded.CharacterId==8 && loaded.Kind==BankStorageKind.PremiumSaddlebag && profile.CharacterId==7,"original profile and destination remain independent");
        }),
    };
}
