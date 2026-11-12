using System.Runtime.InteropServices;

namespace TenshoAssistant.Desktop;

/// <summary>
/// X-163 (0.1154, user request of 12/11): the desktop application's Windows notification when it rings an alarm. A notification-area
/// icon of the application's own window (Shell_NotifyIcon, no package, no network) shows a balloon, which Windows 10 and 11 show as a
/// notification; Windows' own rules (do not disturb, focus) decide whether it appears. No sound of its own (NIIF_NOSOUND): the sound is the
/// « Windows sound » setting's. The icon is added at the first notice and removed when the application closes. Every call is on the UI
/// thread, with the window that owns the icon; a refused call is written to the console and the bar's line stays (never a crash).
/// The structure's layout is checked by DesktopNotice1154Tests (976 bytes on x64, the shell's NOTIFYICONDATAW): a wrong layout would
/// make the shell read past it.
/// </summary>
internal sealed class DesktopNotice
{
    /// <summary>The longest balloon text the shell keeps (szInfo is 256 characters with its final zero).</summary>
    public const int BodyMax=255;
    public const string Title="Tensho Assistant";
    private const uint Id=1;
    private const uint NimAdd=0,NimModify=1,NimDelete=2;
    private const uint NifIcon=0x2,NifTip=0x4,NifInfo=0x10;
    private const uint NiifInfo=0x1,NiifNoSound=0x10;
    private const int IdiInformation=32516;

    private IntPtr window;
    private bool added;

    /// <summary>The balloon's text: the lines one per row, cut at BodyMax with « … » (the bar keeps every line in full).</summary>
    public static string Body(IReadOnlyList<string> lines)
    {
        var text=string.Join("\n",lines.Where(line=>!string.IsNullOrWhiteSpace(line)).Select(line=>line.Trim()));
        return text.Length<=BodyMax ? text : text[..(BodyMax-1)].TrimEnd()+"…";
    }

    /// <summary>The structure the shell reads, filled for one call (only the fields the flags name are read).</summary>
    public static NotifyIconData Data(IntPtr window,uint flags,string body)
        => new()
        {
            Size=(uint)Marshal.SizeOf<NotifyIconData>(),Window=window,Id=Id,Flags=flags,Icon=(flags&NifIcon)!=0 ?LoadIcon(IntPtr.Zero,(IntPtr)IdiInformation) : IntPtr.Zero,
            Tip=Title,Info=body,InfoTitle=Title,InfoFlags=NiifInfo|NiifNoSound,
        };

    /// <summary>Shows the lines as a Windows notification from this window; false when the shell refused it (written to the console).</summary>
    public bool Show(IntPtr owner,IReadOnlyList<string> lines)
    {
        var body=Body(lines);
        if(owner==IntPtr.Zero || body.Length==0)return false;
        try
        {
            if(added && owner==window)
            {
                var modify=Data(owner,NifInfo,body);
                if(Shell_NotifyIconW(NimModify,ref modify))return true;
                added=false; // Explorer restarted: the icon is gone, add it again.
            }
            window=owner;
            var add=Data(owner,NifIcon|NifTip|NifInfo,body);
            added=Shell_NotifyIconW(NimAdd,ref add);
            if(!added)Console.WriteLine("Notice: Windows refused the notification icon; the alarm stays in the bar");
            return added;
        }
        catch(Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            Console.WriteLine($"Notice: not shown ({error.GetType().Name}); the alarm stays in the bar");
            return false;
        }
    }

    /// <summary>Removes the icon (the application closes); nothing when none was added.</summary>
    public void Remove()
    {
        if(!added)return;
        added=false;
        try{var data=Data(window,0,"");Shell_NotifyIconW(NimDelete,ref data);}
        catch(Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException){}
    }

    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    internal struct NotifyIconData
    {
        public uint Size;
        public IntPtr Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string Info;
        public uint TimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Item;
        public IntPtr BalloonIcon;
    }

    [DllImport("shell32",CharSet=CharSet.Unicode)] private static extern bool Shell_NotifyIconW(uint message,ref NotifyIconData data);
    [DllImport("user32")] private static extern IntPtr LoadIcon(IntPtr instance,IntPtr name);
}
