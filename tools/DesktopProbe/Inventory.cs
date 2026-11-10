using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

/// <summary>
/// --inventory: per page, what of Dalamud its draw code can reach, read from the built plugin's IL (Mono.Cecil, from the installed Dalamud).
/// Pages and their draw methods come from MainWindow.PageEntries (the single page list, MainWindow.Pages.cs). From each draw method, the
/// call graph is followed through the plugin's own code (calls, delegates, lambdas, async and iterator state machines); every member of
/// Dalamud (except its ImGui bindings) and of FFXIVClientStructs met on the way is kept, grouped by type. Virtual calls through the plugin's
/// own interfaces are not followed (none of the pages' services are interfaces). "Can reach" is an upper bound: a branch may never run.
/// </summary>
internal static class Inventory
{
    private static readonly string[] Outside=["Dalamud","FFXIVClientStructs","InteropGenerator.Runtime"];

    public static int Run(string plugin,string? output)
    {
        var resolver=new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Program.Hooks);resolver.AddSearchDirectory(Path.GetDirectoryName(plugin)!);
        using var module=ModuleDefinition.ReadModule(plugin,new ReaderParameters{AssemblyResolver=resolver});
        var main=module.GetType("TenshoAssistant.MainWindow");
        var entries=PageMethods(main);
        Console.WriteLine($"pages in MainWindow.PageEntries: {entries.Count}");
        var pageNames=main.NestedTypes.Single(t=>t.Name=="Page").Fields.Where(f=>f.IsLiteral).ToDictionary(f=>Convert.ToInt32(f.Constant),f=>f.Name);
        // Stops: the other pages' draw methods (a link to a page is not that page's code) and the plugin's game-link classes, the types
        // declared in its *.Dalamud.cs files (the project's rule: game-bound code lives there) plus Plugin; reached, they are kept by name.
        var source=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(plugin)!,"..","..",".."));
        var linkTypes=Directory.Exists(source)
            ? Directory.EnumerateFiles(source,"*.Dalamud.cs",SearchOption.AllDirectories).Where(f=>!f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .SelectMany(f=>System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(f),@"\b(?:class|struct|record)\s+([A-Za-z0-9_]+)").Select(m=>m.Groups[1].Value)).ToHashSet()
            : [];
        linkTypes.Add("Plugin");
        Console.WriteLine($"game-link types (from *.Dalamud.cs in {source}): {linkTypes.Count}");
        var draws=entries.Select(e=>e.Method).ToHashSet();
        var rows=new List<(string Page,string Method,SortedDictionary<string,SortedSet<string>> Reached,int Methods)>();
        foreach(var (page,method) in entries)
        {
            var (reached,count)=Reach(method,module,draws,linkTypes);
            rows.Add((pageNames[page],method.Name,reached,count));
        }
        Rows=rows.Select(r=>(r.Page,r.Reached)).ToList();
        var text=new StringBuilder();
        text.AppendLine("| Page | Draw method | Plugin methods reached | Dalamud and ClientStructs types reached (members) |");
        text.AppendLine("|---|---|---|---|");
        foreach(var row in rows)
        {
            var types=string.Join("; ",row.Reached.Select(pair=>$"{pair.Key} ({string.Join(", ",pair.Value.Take(8))}{(pair.Value.Count>8 ? $", +{pair.Value.Count-8}" : "")})"));
            text.AppendLine($"| {row.Page} | {row.Method} | {row.Methods} | {(types.Length==0 ? "-" : types)} |");
            Console.WriteLine($"  {row.Page,-22} {row.Methods,5} methods  {string.Join(", ",row.Reached.Keys)}");
        }
        // Totals per Dalamud type: how many pages reach it.
        text.AppendLine();
        text.AppendLine("| Dalamud or ClientStructs type | Pages that can reach it |");
        text.AppendLine("|---|---|");
        foreach(var group in rows.SelectMany(r=>r.Reached.Keys.Select(k=>(Type:k,r.Page))).GroupBy(p=>p.Type).OrderByDescending(g=>g.Count()).ThenBy(g=>g.Key))
            text.AppendLine($"| {group.Key} | {group.Count()}: {string.Join(", ",group.Select(p=>p.Page))} |");
        if(output is not null)File.WriteAllText(output,text.ToString());
        else Console.WriteLine(text);
        return 0;
    }

    /// <summary>(page value, draw method) from the IL of MainWindow.get_PageEntries: the page constant, then the first MainWindow method taken by ldftn.</summary>
    private static List<(int Page,MethodDefinition Method)> PageMethods(TypeDefinition main)
    {
        var getter=main.Properties.Single(p=>p.Name=="PageEntries").GetMethod;
        var list=new List<(int,MethodDefinition)>();
        // Per element: [array index], the page constant, then the category (ldstr or ldnull): the page is the last constant before it.
        int? page=null;int? last=null;MethodDefinition? draw=null;
        foreach(var instruction in getter.Body.Instructions)
        {
            if(page is null && Constant(instruction) is { } value)last=value;
            else if(page is null && last is not null && instruction.OpCode.Code is Code.Ldstr or Code.Ldnull)page=last;
            else if(page is not null && draw is null && instruction.OpCode.Code==Code.Ldftn && instruction.Operand is MethodReference target && target.DeclaringType.Name=="MainWindow")draw=target.Resolve();
            else if(instruction.OpCode.Code==Code.Newobj && instruction.Operand is MethodReference ctor && ctor.DeclaringType.Name=="PageEntry")
            {
                if(page is { } p && draw is not null)list.Add((p,draw));
                page=null;last=null;draw=null;
            }
        }
        return list;
    }

    private static int? Constant(Instruction instruction)=>instruction.OpCode.Code switch
    {
        Code.Ldc_I4_0=>0,Code.Ldc_I4_1=>1,Code.Ldc_I4_2=>2,Code.Ldc_I4_3=>3,Code.Ldc_I4_4=>4,Code.Ldc_I4_5=>5,Code.Ldc_I4_6=>6,Code.Ldc_I4_7=>7,Code.Ldc_I4_8=>8,
        Code.Ldc_I4_S=>(sbyte)instruction.Operand,Code.Ldc_I4=>(int)instruction.Operand,_=>null,
    };

    public static List<(string Page,SortedDictionary<string,SortedSet<string>> Reached)> Rows=[];

    private static (SortedDictionary<string,SortedSet<string>>,int) Reach(MethodDefinition start,ModuleDefinition module,HashSet<MethodDefinition> draws,HashSet<string> linkTypes)
    {
        var reached=new SortedDictionary<string,SortedSet<string>>(StringComparer.Ordinal);
        var seen=new HashSet<MethodDefinition>();var queue=new Queue<MethodDefinition>();
        static TypeDefinition Outer(TypeDefinition type){while(type.IsNested)type=type.DeclaringType;return type;}
        void Visit(MethodDefinition? method)
        {
            if(method is null || method.Module!=module || (method!=start && draws.Contains(method)))return;
            var owner=Outer(method.DeclaringType).Name.Split('`')[0];
            if(linkTypes.Contains(owner))
            {
                if(!reached.TryGetValue("link:"+owner,out var calls))reached["link:"+owner]=calls=new(StringComparer.Ordinal);
                calls.Add(method.Name.Replace("get_","").Replace("set_",""));
                return;
            }
            if(seen.Add(method))queue.Enqueue(method);
        }
        void Keep(MemberReference member)
        {
            var type=member.DeclaringType;
            if(type is null)return;
            var scope=(type.Scope as AssemblyNameReference)?.Name ?? type.Module?.Assembly?.Name?.Name ?? "";
            if(!Outside.Contains(scope))return;
            var name=type.IsNested ? type.DeclaringType.FullName+"."+type.Name : type.FullName;
            if(name.StartsWith("Dalamud.Bindings.",StringComparison.Ordinal))return;
            name=name.Split('`')[0];
            if(!reached.TryGetValue(name,out var members))reached[name]=members=new(StringComparer.Ordinal);
            members.Add(member.Name.Replace("get_","").Replace("set_",""));
        }
        Visit(start);
        while(queue.TryDequeue(out var method))
        {
            // Async and iterator methods: their body is the state machine's MoveNext.
            foreach(var attribute in method.CustomAttributes.Where(a=>a.AttributeType.Name is "AsyncStateMachineAttribute" or "IteratorStateMachineAttribute"))
                if(attribute.ConstructorArguments[0].Value is TypeReference machine)Visit(machine.Resolve()?.Methods.FirstOrDefault(m=>m.Name=="MoveNext"));
            if(!method.HasBody)continue;
            foreach(var instruction in method.Body.Instructions)
                switch(instruction.Operand)
                {
                    case MethodReference called:
                        if(Resolve(called) is { } definition && definition.Module==module)Visit(definition);
                        else Keep(called);
                        break;
                    case FieldReference field:
                        if(Owner(field.DeclaringType)!=module)Keep(field);
                        break;
                }
        }
        return (reached,seen.Count);
    }

    private static MethodDefinition? Resolve(MethodReference method)
    {
        try{return method.Resolve();}catch{return null;}
    }

    private static ModuleDefinition? Owner(TypeReference type)
    {
        try{return type.Resolve()?.Module;}catch{return null;}
    }
}
