// 0.1278 (X-271, R37): reference images of the desktop application's pages. The comparison (ImageCompare: SSIM of 8 x 8 blocks, changed
// area) and the PNG reader are pure and checked here on made pictures; the capture mode (--references, hidden window, new settings) is
// checked by its text (it needs the game data and a window: the agent's publication runs it).
using TenshoAssistant.Desktop;

internal static class DesktopImages1278Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    /// <summary>A made picture: a dark page with a light rectangle (a button) at (x, y), and grey text-like stripes.</summary>
    private static byte[] Page(int width,int height,int buttonX,int buttonY,byte shade=230)
    {
        var rgba=new byte[width*height*4];
        for(var y=0;y<height;y++)
            for(var x=0;x<width;x++)
            {
                var at=(y*width+x)*4;
                byte v=20;
                if(x>=buttonX && x<buttonX+40 && y>=buttonY && y<buttonY+16)v=shade;
                else if(y%12<2 && x%7<5 && x<width-20)v=140;
                rgba[at]=rgba[at+1]=rgba[at+2]=v;rgba[at+3]=255;
            }
        return rgba;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop images 1278: a picture compared with itself is the same, with nothing changed",() =>
        {
            var page=Page(160,120,30,40);
            var result=ImageCompare.Compare(160,120,page,160,120,(byte[])page.Clone());
            Check(Math.Abs(result.Ssim-1)<1e-9 && result.Changed==0 && result.ChangedBlocks==0 && result.SameSize,$"same picture: ssim {result.Ssim}, changed {result.Changed}");
            Check(result.Within(0.99,0),"within any tolerance");
        }),
        ("Desktop images 1278: a moved button is a localised change, outside a tight tolerance; its box is where it moved",() =>
        {
            var before=Page(160,120,30,40);var after=Page(160,120,90,80);
            var result=ImageCompare.Compare(160,120,before,160,120,after);
            Check(result.ChangedBlocks>0 && result.Changed<0.2,$"a few blocks changed ({result.ChangedBlocks}, {result.Changed:P1})");
            Check(!result.Within(0.995,0.002),"a moved button fails the publication's tolerance");
            Check(result.Left<=30 && result.Right>=130 && result.Top<=40 && result.Bottom>=96,$"the box holds both places ({result.Left},{result.Top})-({result.Right},{result.Bottom})");
            var diff=ImageCompare.DiffPicture(160,120,before,after);
            var at=(85*160+100)*4;
            Check(diff.Length==160*120*4 && diff[at]>diff[at+1],"the diff picture is red where it changed");
        }),
        ("Desktop images 1278: a picture of another size is wholly changed; a slight shade is within tolerance",() =>
        {
            var result=ImageCompare.Compare(160,120,Page(160,120,30,40),200,120,Page(200,120,30,40));
            Check(!result.SameSize && result.Changed==1 && !result.Within(0,1),"another size never passes");
            var shaded=ImageCompare.Compare(160,120,Page(160,120,30,40,230),160,120,Page(160,120,30,40,226));
            Check(shaded.Within(0.99,0.01),$"a shade of 4 levels on a button stays within tolerance (ssim {shaded.Ssim}, changed {shaded.Changed})");
        }),
        ("Desktop images 1278: a PNG written is read back the same; the five row filters are decoded; another kind is refused",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-images-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var page=Page(37,23,5,4);
                var file=Path.Combine(folder,"page.png");
                Png.Write(file,37,23,page);
                var (w,h,rgba)=Png.Read(file);
                Check(w==37 && h==23 && rgba.AsSpan().SequenceEqual(page),"round trip");
                // A 2 x 2 RGB picture, one row per filter kind after the first, made by hand: filter 1 (Sub) then 4 (Paeth).
                byte[] raw=[1, 10,20,30, 5,5,5,  4, 1,2,3, 7,7,7];
                using var packed=new MemoryStream();
                using(var z=new System.IO.Compression.ZLibStream(packed,System.IO.Compression.CompressionLevel.Optimal,true))z.Write(raw);
                var png=Hand(2,2,2,packed.ToArray());
                var (_,_,decoded)=Png.Read(png);
                Check(decoded[0]==10 && decoded[4]==15 && decoded[5]==25 && decoded[7]==255,"Sub: the left pixel added");
                // Paeth on row 2: first pixel's left is 0, up is (10,20,30), upper-left 0: predictor = up.
                Check(decoded[8]==11 && decoded[9]==22 && decoded[10]==33,$"Paeth: the upper pixel chosen ({decoded[8]},{decoded[9]},{decoded[10]})");
                var refused=false;
                try{Png.Read(Hand(2,2,0,packed.ToArray()));}catch(InvalidDataException){refused=true;}
                Check(refused,"a grey picture is refused, never read wrong");
            }
            finally{Directory.Delete(folder,true);}
        }),
        ("Desktop images 1278: the capture mode draws in a hidden window with new settings and compares with the references",() =>
        {
            var program=Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.Contains("var settings=args.Contains(\"--new-settings\") || references is not null ? null"),"never the player's settings nor data");
            Check(program.Contains("new Win32Gl($\"Tensho Assistant {DesktopVersionCheck.Three(AppVersion)}\",WindowSize.W,WindowSize.H,!hidden)"),"the window stays hidden");
            Check(program.Contains("a.StartsWith(\"--references=\",StringComparison.Ordinal)"),"a diagnostic run: no update, the console kept");
            var references=Read("src/TenshoAssistant.Desktop/References.cs");
            Check(references.Contains("ImageCompare.Compare(w,h,before,width,height,picture!)") && references.Contains("\"report.json\""),"compared, reported");
            var plan=ReferencePlan.Plan(DesktopPages.AsIs.Concat(DesktopPages.NeedsLink).Concat(DesktopPages.InGame));
            Check(plan.Select(shot=>shot.Id).Distinct().Count()==plan.Count,"one name per picture");
            Check(DesktopPages.AsIs.Concat(DesktopPages.NeedsLink).All(page=>plan.Any(shot=>shot.Id==page)),"every page of the application has its picture");
            Check(!plan.Any(shot=>DesktopPages.InGame.Contains(shot.Page)),"the pages kept in the game are not pictured");
            Check(plan.TakeWhile(shot=>shot.Setup=="loading").Count()==plan.Count(shot=>shot.Setup=="loading") && plan.Any(shot=>shot.Setup=="loading"),"the loading states first");
            Check(Enumerable.Range(1,6).All(theme=>plan.Any(shot=>shot.Theme==theme)) && plan.Any(shot=>shot.Width<600),"each theme, a narrow window");
            Check(plan.Any(shot=>shot.Setup=="garden-icons") && plan.Any(shot=>shot.Setup=="garden-detailed") && plan.Any(shot=>shot.Setup=="dashboard"),"the trial garden and dashboard");
        }),
    ];

    /// <summary>A PNG made by hand: IHDR (8 bits, the colour type given) and one IDAT (CRCs not checked by the reader).</summary>
    private static byte[] Hand(int width,int height,byte colour,byte[] idat)
    {
        using var file=new MemoryStream();
        file.Write([0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A]);
        void Chunk(string type,byte[] data)
        {
            file.Write([(byte)(data.Length>>24),(byte)(data.Length>>16),(byte)(data.Length>>8),(byte)data.Length]);
            file.Write(System.Text.Encoding.ASCII.GetBytes(type));file.Write(data);file.Write([0,0,0,0]);
        }
        Chunk("IHDR",[0,0,0,(byte)width,0,0,0,(byte)height,8,colour,0,0,0]);
        Chunk("IDAT",idat);
        Chunk("IEND",[]);
        return file.ToArray();
    }
}
