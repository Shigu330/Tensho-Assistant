namespace TenshoAssistant.Desktop;

/// <summary>
/// X-271 (R37): two pictures of a page compared with tolerance. The structural similarity (SSIM, Wang, Bovik, Sheikh and Simoncelli,
/// IEEE Transactions on Image Processing 13(4), 2004: luminance, contrast and structure of each window, constants C1 = (0.01 L)^2 and
/// C2 = (0.03 L)^2 with L = 255) on the luminance (Rec. 601 weights) of 8 x 8 blocks side by side; the mean over the picture, and the
/// part of the picture whose blocks fall under <see cref="ChangedBelow"/> (a moved button or a new line shows there even when the mean
/// stays high; a clock that ticks changes a few blocks). Pictures of different sizes are wholly changed. Pure: no file, no window.
/// </summary>
public static class ImageCompare
{
    public const int Block=8;
    /// <summary>A block whose SSIM is under this counts as changed.</summary>
    public const double ChangedBelow=0.90;
    private const double C1=6.5025,C2=58.5225;

    public sealed record Result(double Ssim,double Changed,int ChangedBlocks,int Blocks,int Left,int Top,int Right,int Bottom,bool SameSize)
    {
        /// <summary>Within the tolerance: mean SSIM at least <paramref name="minSsim"/> and changed area at most <paramref name="maxChanged"/>.</summary>
        public bool Within(double minSsim,double maxChanged)=>SameSize && Ssim>=minSsim && Changed<=maxChanged;
    }

    public static Result Compare(int width,int height,byte[] before,int otherWidth,int otherHeight,byte[] after)
    {
        if(width!=otherWidth || height!=otherHeight)return new(0,1,0,0,0,0,Math.Max(width,otherWidth),Math.Max(height,otherHeight),false);
        if(before.Length<width*height*4 || after.Length<width*height*4)throw new ArgumentException("picture shorter than its size");
        var columns=(width+Block-1)/Block;var rows=(height+Block-1)/Block;
        double total=0;var changed=0;int left=int.MaxValue,top=int.MaxValue,right=-1,bottom=-1;
        for(var by=0;by<rows;by++)
            for(var bx=0;bx<columns;bx++)
            {
                var ssim=BlockSsim(width,height,before,after,bx*Block,by*Block);
                total+=ssim;
                if(ssim<ChangedBelow)
                {
                    changed++;
                    left=Math.Min(left,bx*Block);top=Math.Min(top,by*Block);
                    right=Math.Max(right,Math.Min(width,bx*Block+Block));bottom=Math.Max(bottom,Math.Min(height,by*Block+Block));
                }
            }
        var blocks=columns*rows;
        return changed==0 ? new(total/blocks,0,0,blocks,0,0,0,0,true) : new(total/blocks,(double)changed/blocks,changed,blocks,left,top,right,bottom,true);
    }

    /// <summary>The SSIM of one block (its part inside the picture), 1 for two equal blocks.</summary>
    public static double BlockSsim(int width,int height,byte[] before,byte[] after,int x0,int y0)
    {
        var x1=Math.Min(width,x0+Block);var y1=Math.Min(height,y0+Block);
        double sumA=0,sumB=0,sumAA=0,sumBB=0,sumAB=0;var n=0;
        for(var y=y0;y<y1;y++)
            for(var x=x0;x<x1;x++)
            {
                var at=(y*width+x)*4;
                var a=Luma(before,at);var b=Luma(after,at);
                sumA+=a;sumB+=b;sumAA+=a*a;sumBB+=b*b;sumAB+=a*b;n++;
            }
        if(n==0)return 1;
        var meanA=sumA/n;var meanB=sumB/n;
        var varA=Math.Max(0,sumAA/n-meanA*meanA);var varB=Math.Max(0,sumBB/n-meanB*meanB);var cov=sumAB/n-meanA*meanB;
        return (2*meanA*meanB+C1)*(2*cov+C2)/((meanA*meanA+meanB*meanB+C1)*(varA+varB+C2));
    }

    private static double Luma(byte[] rgba,int at)=>0.299*rgba[at]+0.587*rgba[at+1]+0.114*rgba[at+2];

    /// <summary>The new picture dimmed, its changed blocks tinted red: where to look, for the gallery.</summary>
    public static byte[] DiffPicture(int width,int height,byte[] before,byte[] after)
    {
        var picture=new byte[width*height*4];
        for(var at=0;at<picture.Length;at+=4)
        {
            var grey=(byte)(Luma(after,at)*0.45);
            picture[at]=picture[at+1]=picture[at+2]=grey;picture[at+3]=255;
        }
        for(var y0=0;y0<height;y0+=Block)
            for(var x0=0;x0<width;x0+=Block)
            {
                if(BlockSsim(width,height,before,after,x0,y0)>=ChangedBelow)continue;
                for(var y=y0;y<Math.Min(height,y0+Block);y++)
                    for(var x=x0;x<Math.Min(width,x0+Block);x++)
                    {
                        var at=(y*width+x)*4;
                        picture[at]=(byte)Math.Min(255,120+Luma(after,at)*0.55);picture[at+1]=(byte)(Luma(after,at)*0.25);picture[at+2]=(byte)(Luma(after,at)*0.25);
                    }
            }
        return picture;
    }
}
