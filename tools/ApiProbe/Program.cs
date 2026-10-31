using System.Reflection;
using System.Runtime.InteropServices;

// Metadata only: no host code or native game state is executed.
var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher/addon/Hooks/dev");
var paths = Directory.GetFiles(directory, "*.dll")
    .Concat(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
using var context = new MetadataLoadContext(new PathAssemblyResolver(paths));
var assembly = context.LoadFromAssemblyPath(Path.Combine(directory, "Dalamud.dll"));
foreach (var name in args)
{
    if(name.StartsWith("--find-method=",StringComparison.Ordinal))
    {
        var structs=context.LoadFromAssemblyPath(Path.Combine(directory,"FFXIVClientStructs.dll"));
        var search=name["--find-method=".Length..];
        foreach(var found in structs.GetTypes().Where(type=>type.FullName is { } full && !full.Contains("Delegates") && !full.Contains("VirtualTable") && !full.Contains("MemberFunctionPointers")))
        foreach(var method in found.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method=>!method.IsSpecialName && method.Name.Contains(search,StringComparison.OrdinalIgnoreCase)))
            Console.WriteLine($"{found.FullName}: {method}");
        continue;
    }
    if(name.StartsWith("--find-struct=",StringComparison.Ordinal))
    {
        var structs=context.LoadFromAssemblyPath(Path.Combine(directory,"FFXIVClientStructs.dll"));
        var search=name["--find-struct=".Length..];
        foreach(var found in structs.GetTypes().Where(type=>type.FullName?.Contains(search,StringComparison.OrdinalIgnoreCase)==true && !type.FullName.Contains("Delegates") && !type.FullName.Contains("VirtualTable")))
            Console.WriteLine(found.FullName);
        continue;
    }
    if(name.StartsWith("--find-excel=",StringComparison.Ordinal))
    {
        var excel=context.LoadFromAssemblyPath(Path.Combine(directory,"Lumina.Excel.dll"));
        foreach(var found in excel.GetTypes().Where(type=>type.Namespace=="Lumina.Excel.Sheets" && type.Name.Contains(name["--find-excel=".Length..],StringComparison.OrdinalIgnoreCase)))
            Console.WriteLine(found.FullName);
        continue;
    }
    if (name == "--find-triad")
    {
        var structs = context.LoadFromAssemblyPath(Path.Combine(directory, "FFXIVClientStructs.dll"));
        foreach (var found in structs.GetTypes().Where(type => type.FullName?.Contains("Triad", StringComparison.OrdinalIgnoreCase) == true
            && !type.FullName.Contains("Delegates", StringComparison.Ordinal) && !type.FullName.Contains("VirtualTable", StringComparison.Ordinal)))
            Console.WriteLine(found.FullName);
        continue;
    }
    var library = name.StartsWith("FFXIVClientStructs.", StringComparison.Ordinal) ? "FFXIVClientStructs.dll"
        : name.StartsWith("Lumina.Excel.Sheets.", StringComparison.Ordinal) ? "Lumina.Excel.dll"
        : name.StartsWith("Lumina.", StringComparison.Ordinal) ? "Lumina.dll" : "Dalamud.dll";
    var targetAssembly = library == "Dalamud.dll" ? assembly
        : context.LoadFromAssemblyPath(Path.Combine(directory, library));
    var type = targetAssembly.GetType(name, true)!;
    Console.WriteLine($"TYPE {type}");
    foreach (var attribute in type.GetCustomAttributesData()) Console.WriteLine($"ATTRIBUTE {attribute}");
    foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
    {
        if (member is MethodInfo method && method.IsSpecialName) continue;
        Console.WriteLine($"  {member}");
        if (member is FieldInfo field && field.IsLiteral) Console.WriteLine($"    = {field.GetRawConstantValue()}");
    }
}
