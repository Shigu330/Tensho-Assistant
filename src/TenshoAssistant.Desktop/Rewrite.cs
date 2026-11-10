using System.Security.Cryptography;
using Dalamud.Bindings.ImGui;
using Mono.Cecil;
using Mono.Cecil.Cil;

/// <summary>
/// Dalamud's font statics (UiBuilder.IconFont, IconFontFixedWidth, DefaultFont, MonoFont) wait for Dalamud's interface service, which never
/// starts outside the game: the first call blocks forever (measured 2026-11-10: InterfaceManager.WhenFontsReady → Service&lt;InterfaceManager&gt;,
/// docs/evidence/x118-desktop-probe.md). The app copies the built plugin to its own folder (%LOCALAPPDATA%\TenshoAssistant\Desktop\plugin,
/// never the repository nor the installed plugin) and points those calls at the desktop fonts below, with Mono.Cecil (read from Dalamud's
/// install, not shipped).
/// TEMPORARY (X-119 step 1): this rewrite is replaced by a real seam in the plugin after the post-7.58 release (the fonts read from one
/// property of the plugin's UI components, set by Plugin.cs from UiBuilder and by this app from its atlas; a ratchet test forbids new
/// direct uses). Until then the plugin's source is not changed (frozen until Dalamud is ready for 7.58).
/// </summary>
internal static class Rewrite
{
    public static readonly Dictionary<string,int> Sites=[];
    /// <summary>The plugin's own files the copy carries beside the DLL (as in latest.zip).</summary>
    private static readonly string[] Siblings=["Raphael.Net.dll","raphael_bindings.dll","TenshoAssistant.json"];

    /// <summary>The rewritten copy of the plugin, in a folder named after the DLL's content (two apps can run at once, a copy in use is never overwritten).</summary>
    public static string Plugin(string plugin,string root)
    {
        var hash=Convert.ToHexString(SHA256.HashData([..File.ReadAllBytes(plugin),..System.Text.Encoding.UTF8.GetBytes(Program.AppVersion.ToString())]))[..16];
        var folder=Path.Combine(root,hash);
        Directory.CreateDirectory(folder);
        foreach(var old in Directory.GetDirectories(root).Where(d=>!string.Equals(d,folder,StringComparison.OrdinalIgnoreCase)))
            try{Directory.Delete(old,true);}catch(IOException){}catch(UnauthorizedAccessException){}// in use by another run: left for later
        foreach(var sibling in Siblings)
        {
            var source=Path.Combine(Path.GetDirectoryName(plugin)!,sibling);
            var copy=Path.Combine(folder,sibling);
            if(File.Exists(source) && !File.Exists(copy))File.Copy(source,copy);
        }
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
        if(!File.Exists(target))module.Write(target);
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
