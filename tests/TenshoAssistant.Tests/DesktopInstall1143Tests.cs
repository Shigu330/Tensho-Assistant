using TenshoAssistant.Desktop;
using TenshoAssistant.Link;

// 0.1143 (X-170, GPT audit of 12/11, installer part). Behaviour tests (R28): a damaged update.json is never taken for a new install (not written
// over, no copy removed, no existing folder replaced); leftovers removed only when Tensho named them; the previous copy started when Windows
// refuses the current one.
internal static class DesktopInstall1143Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-install-1143-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{run(root);}finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    private static string Copy(string versions,string name,bool marker,bool exe)
    {
        var path=Path.Combine(versions,name);Directory.CreateDirectory(path);
        if(marker)File.WriteAllText(Path.Combine(path,DesktopInstall.Marker),name);
        if(exe)File.WriteAllText(Path.Combine(path,DesktopUpdate.ExeName),"exe");
        return path;
    }
    private const string Unique="0123456789abcdef0123456789abcdef";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop install 1143: a damaged update.json is not a new install: not written over, no copy removed, no existing folder replaced",() => InTemp(root =>
        {
            var own=Path.Combine(root,"own");var versions=DesktopInstall.VersionsFolder(own);
            var current=Copy(versions,"0.1142.0",true,true);var previous=Copy(versions,"0.1141.0",true,true);var running=Copy(versions,"0.1140.0",true,true);
            Directory.CreateDirectory(own);File.WriteAllText(DesktopInstall.StatePath(own),"{ \"Current\": { damaged");
            var bytes=File.ReadAllBytes(DesktopInstall.StatePath(own));
            Check(DesktopInstall.LoadReadable(own) is null && DesktopInstall.Load(own).Current is null,"unreadable: null (Load still gives an empty state to show)");
            var refused=false;
            try{DesktopInstall.Update(own,state=>state with {AutoUpdate=false});}catch(UpdateStateUnreadableException){refused=true;}
            Check(refused && File.ReadAllBytes(DesktopInstall.StatePath(own)).SequenceEqual(bytes),"not written over");
            Check(refused && new UpdateStateUnreadableException("x") is IOException,"an IOException: every caller already handles it");
            var removed=DesktopInstall.Clean(own,running);
            Check(removed.Count==0 && Directory.Exists(current) && Directory.Exists(previous),"no copy removed while Current and Previous are unknown");
            var partial=Copy(versions,"0.1142.0."+Unique+".partial",true,true);
            Check(DesktopInstall.Place(partial,current,new Version(0,1142,0),null,running)==DesktopInstall.Placement.Refused,"an existing copy that does not check is refused, never replaced");
            Check(File.Exists(Path.Combine(current,DesktopInstall.Marker)),"the existing copy is still there, untouched");
            Check(DesktopInstall.Place(partial,Path.Combine(versions,"0.1143.0"),new Version(0,1143,0),null,running)==DesktopInstall.Placement.Moved,"a free folder still receives the copy");
            Check(DesktopUpdate.TryReadState(null,out var empty) && empty.Current is null && !DesktopUpdate.TryReadState("[1]",out _),"missing = new install; damaged = refused");
        })),
        ("Desktop install 1143: the damaged copy in place is refused (not replaced) when update.json cannot be read",() => InTemp(root =>
        {
            var versions=Path.Combine(root,"versions");
            var damaged=Copy(versions,"0.1143.0",true,false);
            var partial=Copy(versions,"0.1143.0."+Unique+".partial",true,true);
            Check(DesktopInstall.Place(partial,damaged,new Version(0,1143,0),null,null)==DesktopInstall.Placement.Refused,"unknown state: every copy counts as in use");
            Check(Directory.Exists(damaged) && File.Exists(Path.Combine(damaged,DesktopInstall.Marker)) && Directory.Exists(partial),"nothing moved");
            Check(DesktopInstall.Place(partial,damaged,new Version(0,1143,0),new UpdateState(),null)==DesktopInstall.Placement.Moved,"with a readable state, Tensho's unused damaged copy is replaced as before");
        })),
        ("Desktop install 1143: leftovers removed only when Tensho named them; a folder of the user's ending in .old or .partial is kept",() => InTemp(root =>
        {
            var own=Path.Combine(root,"own");var versions=DesktopInstall.VersionsFolder(own);
            var current=Copy(versions,"0.1142.0",true,true);
            DesktopInstall.Update(own,_=>new UpdateState(Current:new("0.1142.0",current)));
            var tenshoOld=Copy(versions,"0.1139.0."+Unique+".old",true,true);
            var tenshoPartial=Copy(versions,"0.1141.0."+Unique+".partial",false,false);Directory.SetCreationTimeUtc(tenshoPartial,DateTime.UtcNow.AddHours(-2));
            var freshPartial=Copy(versions,"0.1142.0."+Unique.Replace('0','1')+".partial",false,false);
            var userOld=Copy(versions,"photos.old",false,false);File.WriteAllText(Path.Combine(userOld,"mine.txt"),"x");
            var userPartial=Copy(versions,"save.partial",false,true);Directory.SetCreationTimeUtc(userPartial,DateTime.UtcNow.AddHours(-2));
            var namedButForeign=Copy(versions,"x."+Unique+".old",false,false);File.WriteAllText(Path.Combine(namedButForeign,"mine.txt"),"x");
            var downloads=Path.Combine(own,"downloads");Directory.CreateDirectory(downloads);
            var tenshoZip=Path.Combine(downloads,DesktopUpdate.ZipName+"."+Unique+".partial");File.WriteAllText(tenshoZip,"z");
            var userFile=Path.Combine(downloads,"film.partial");File.WriteAllText(userFile,"z");
            DesktopInstall.Clean(own,current);
            Check(!Directory.Exists(tenshoOld) && !Directory.Exists(tenshoPartial) && !File.Exists(tenshoZip),"Tensho's leftovers removed");
            Check(Directory.Exists(freshPartial),"an install under way (less than an hour) kept");
            Check(File.Exists(Path.Combine(userOld,"mine.txt")) && Directory.Exists(userPartial) && File.Exists(Path.Combine(namedButForeign,"mine.txt")) && File.Exists(userFile),"the user's folders and files kept");
        })),
        ("Desktop install 1143: Windows refuses the current copy: the previous one, kept in update.json, is started instead",() =>
        {
            var state=new UpdateState(Current:new("0.1143.0",@"C:\v\0.1143.0"),Previous:new("0.1142.0",@"C:\v\0.1142.0"));
            Check(DesktopInstallRules.Fallback(state,@"C:\v\0.1143.0",_=>true)?.Version=="0.1142.0","the previous copy");
            Check(DesktopInstallRules.Fallback(state,@"C:\v\0.1143.0",_=>false) is null,"its exe gone: none");
            Check(DesktopInstallRules.Fallback(state with {Previous=null},@"C:\v\0.1143.0",_=>true) is null,"no previous: none");
            Check(DesktopInstallRules.Fallback(state with {Previous=state.Current},@"C:\v\0.1143.0",_=>true) is null,"the same folder: none");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Link/DesktopInstaller.cs"));
            Check(plugin.Contains("if(DesktopInstallRules.Fallback(State,view.Folder,File.Exists) is { } previous)",StringComparison.Ordinal),"wired in « Open »");
        }),
    ];
}
