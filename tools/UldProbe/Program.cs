using System.Collections;
using System.Reflection;
using Lumina;
using Lumina.Data.Files;

// Read-only probe (step e, game frames): which interface layouts (.uld) exist in the installed game, and which textures each one
// uses. The plugin may only load paths this probe proved (project rule: never invent game data).
if(args.Length<1)throw new ArgumentException("Supply the installed game's sqpack directory.");
using var data=new GameData(args[0]);
// 0.1038: --fdt game-path: the characters a game font (.fdt) has a glyph for, one code point per line (U+XXXX). Dalamud's default
// font and Tensho's game fonts are the game's AXIS family, so a text character missing here shows as the fallback mark.
if(args.Length>2 && args[1]=="--fdt")
{
    var bytes=data.GetFile(args[2])!.Data;
    var table=BitConverter.ToInt32(bytes,0x08);
    var count=BitConverter.ToInt32(bytes,table+0x04);
    for(var index=0;index<count;index++)
    {
        var packed=BitConverter.ToUInt32(bytes,table+0x20+index*0x10);
        var utf8=BitConverter.GetBytes(packed).Reverse().SkipWhile(value=>value==0).ToArray();
        var text=System.Text.Encoding.UTF8.GetString(utf8);
        if(text.Length>0)Console.WriteLine($"U+{char.ConvertToUtf32(text,0):X4}");
    }
    return;
}
// --parts layout texture-prefix: the parts (U, V, width, height) the game cuts from those textures in that layout.
if(args.Length>3 && args[1]=="--parts")
{
    var uld=data.GetFile<UldFile>($"ui/uld/{args[2]}.uld")!;
    var ids=new Dictionary<uint,string>();
    foreach(var asset in uld.AssetData){var text=new string(asset.Path).TrimEnd('\0');if(text.Contains(args[3],StringComparison.OrdinalIgnoreCase))ids[asset.Id]=text;}
    foreach(var list in uld.Parts)
        foreach(var part in list.Parts)
            if(ids.TryGetValue(part.TextureId,out var tex))Console.WriteLine($"PART list {list.Id} {tex} U={part.U} V={part.V} W={part.W} H={part.H}");
    return;
}
// --tex name1 name2…: size of each texture and of its high-resolution variant (_hr1), used to cut the frame pieces.
if(args.Length>1 && args[1]=="--tex")
{
    foreach(var name in args[2..])
        foreach(var path in new[]{$"ui/uld/{name}.tex",$"ui/uld/{name}_hr1.tex"})
            Console.WriteLine(data.GetFile<TexFile>(path) is { } tex ? $"TEX {path} {tex.Header.Width}x{tex.Header.Height}" : $"ABSENT {path}");
    return;
}
var candidates=args.Length>1 ? args[1..] : new[]{"Window","WindowA","RecipeNoteBook","Inventory","ItemDetail","ContentsFinder","Character","ConfigSystem",
    "Journal","AreaMap","Gathering","Talk","ToolTipS","Tooltip","Fashion","Shop","Teleport","ChatLog","Retainer","LetterList"};
foreach(var name in candidates)
{
    var path=$"ui/uld/{name}.uld";
    if(!data.FileExists(path)){Console.WriteLine($"ABSENT {path}");continue;}
    var file=data.GetFile<UldFile>(path)!;
    Console.WriteLine($"PRESENT {path}");
    // Textures: the asset entries hold the .tex paths (read by reflection so the probe does not depend on Lumina member names).
    foreach(var field in typeof(UldFile).GetFields(BindingFlags.Public|BindingFlags.Instance))
    {
        if(field.GetValue(file) is not IEnumerable list || field.FieldType==typeof(string))continue;
        foreach(var item in list)
        {
            if(item is null)continue;
            foreach(var inner in item.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))
            {
                var value=inner.GetValue(item);
                var text=value is char[] chars ? new string(chars).TrimEnd('\0') : value?.ToString();
                if(text is { Length:>0 } && text.EndsWith(".tex",StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine($"  {field.Name}.{inner.Name}: {text} exists={data.FileExists(text)}");
            }
        }
    }
}
