using Dalamud.Bindings.ImGui;
using Mono.Cecil;
using Mono.Cecil.Cil;

/// <summary>
/// Dalamud's font statics (UiBuilder.IconFont, IconFontFixedWidth, DefaultFont, MonoFont) wait for Dalamud's interface service, which never
/// starts outside the game: the first call blocks forever (measured 2026-11-10: InterfaceManager.WhenFontsReady → Service&lt;InterfaceManager&gt;).
/// The probe copies the built plugin to a temporary folder (never into the repository) and points those calls at the desktop fonts below,
/// with Mono.Cecil. In the real application this is one seam in the plugin's own code (the fonts read from UiComponents, set by the
/// plugin from UiBuilder and by the application from its atlas), not a rewrite: the probe only counts the sites and proves the rest draws.
/// </summary>
internal static class Rewrite
{
    public static readonly Dictionary<string,int> Sites=[];

    public static string Plugin(string plugin)
    {
        var folder=Path.Combine(Path.GetTempPath(),"TenshoDesktopProbe","plugin");
        Directory.CreateDirectory(folder);
        var target=Path.Combine(folder,Path.GetFileName(plugin));
        var resolver=new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Program.Hooks);resolver.AddSearchDirectory(Path.GetDirectoryName(plugin)!);
        using var module=ModuleDefinition.ReadModule(plugin,new ReaderParameters{AssemblyResolver=resolver,InMemory=true});
        var map=new Dictionary<string,MethodReference>
        {
            ["get_IconFont"]=module.ImportReference(typeof(DesktopFonts).GetProperty(nameof(DesktopFonts.IconFont))!.GetMethod!),
            ["get_IconFontFixedWidth"]=module.ImportReference(typeof(DesktopFonts).GetProperty(nameof(DesktopFonts.IconFont))!.GetMethod!),
            ["get_DefaultFont"]=module.ImportReference(typeof(DesktopFonts).GetProperty(nameof(DesktopFonts.DefaultFont))!.GetMethod!),
            ["get_MonoFont"]=module.ImportReference(typeof(DesktopFonts).GetProperty(nameof(DesktopFonts.DefaultFont))!.GetMethod!),
        };
        foreach(var type in module.GetTypes())
            foreach(var method in type.Methods.Where(m=>m.HasBody))
                foreach(var instruction in method.Body.Instructions)
                    if(instruction.OpCode.Code is Code.Call && instruction.Operand is MethodReference called
                       && called.DeclaringType.FullName is "Dalamud.Interface.UiBuilder" or "Dalamud.Interface.Internal.InterfaceManager"
                       && map.TryGetValue(called.Name,out var replacement))
                    {
                        instruction.Operand=replacement;
                        var key=called.DeclaringType.Name+"."+called.Name.Replace("get_","");
                        Sites[key]=Sites.GetValueOrDefault(key)+1;
                    }
        module.Write(target);
        return target;
    }
}

/// <summary>The desktop window's fonts: one atlas font (Noto Sans CJK JP Medium with Font Awesome merged), standing for Dalamud's.</summary>
public static class DesktopFonts
{
    public static ImFontPtr Main;
    public static ImFontPtr IconFont=>Main;
    public static ImFontPtr DefaultFont=>Main;
}
