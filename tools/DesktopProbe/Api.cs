using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>--api=Type[:filter]: members of a type of the installed Dalamud assemblies, by metadata only (ApiProbe's way, R06).</summary>
internal static class Api
{
    /// <summary>--il=Type::Method: the IL of a Dalamud method (Mono.Cecil), to see what a static reaches.</summary>
    public static int Il(string spec)
    {
        var parts=spec.Split("::");
        var resolver=new Mono.Cecil.DefaultAssemblyResolver();resolver.AddSearchDirectory(Program.Hooks);
        using var module=Mono.Cecil.ModuleDefinition.ReadModule(Path.Combine(Program.Hooks,"Dalamud.dll"),new Mono.Cecil.ReaderParameters{AssemblyResolver=resolver});
        foreach(var type in module.GetTypes().Where(t=>t.FullName==parts[0]))
            foreach(var method in type.Methods.Where(m=>m.Name==parts[1] && m.HasBody))
            {
                Console.WriteLine(method.FullName);
                foreach(var i in method.Body.Instructions)Console.WriteLine("  "+i);
            }
        return 0;
    }

    public static int Dump(string spec)
    {
        var (typeName,filter)=spec.Split(':',2) is [var t,var f] ? (t,f) : (spec,"");
        var paths=Directory.GetFiles(Program.Hooks,"*.dll").Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(),"*.dll"))
            .GroupBy(Path.GetFileName,StringComparer.OrdinalIgnoreCase).Select(group=>group.First());
        using var context=new MetadataLoadContext(new PathAssemblyResolver(paths));
        foreach(var file in new[]{"Dalamud.Bindings.ImGui.dll","Dalamud.dll","HexaGen.Runtime.dll","Lumina.dll"})
        {
            var assembly=context.LoadFromAssemblyPath(Path.Combine(Program.Hooks,file));
            foreach(var type in assembly.GetTypes().Where(type=>type.FullName==typeName || type.Name==typeName))
            {
                Console.WriteLine($"{type.FullName} ({file}) : {type.BaseType?.Name}");
                foreach(var member in type.GetMembers(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly)
                    .Where(member=>filter.Length==0 || member.Name.Contains(filter,StringComparison.OrdinalIgnoreCase)))
                    Console.WriteLine("  "+member);
            }
        }
        return 0;
    }
}
