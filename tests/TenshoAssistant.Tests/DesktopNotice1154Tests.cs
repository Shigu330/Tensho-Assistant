using System.Runtime.InteropServices;
using TenshoAssistant.Alarms;
using TenshoAssistant.Desktop;

// 0.1154 (X-163, user request of 12/11): the desktop application shows a Windows notification when it rings an alarm. The structure the
// shell reads must have the shell's own layout (NOTIFYICONDATAW, 976 bytes on x64): a wrong size or offset would make Shell_NotifyIcon read
// past it or refuse it. The balloon's text is bounded; the setting is on by default, kept by Copy, read by its name; never in a headless run.
internal static class DesktopNotice1154Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop notice: the structure has the shell's NOTIFYICONDATAW layout (x64)",() =>
        {
            Check(IntPtr.Size==8,"the application and its tests are x64");
            Check(Marshal.SizeOf<DesktopNotice.NotifyIconData>()==976,"size "+Marshal.SizeOf<DesktopNotice.NotifyIconData>());
            int At(string member)=>(int)Marshal.OffsetOf<DesktopNotice.NotifyIconData>(member);
            Check(At("Window")==8 && At("Id")==16 && At("Flags")==20 && At("Icon")==32 && At("Tip")==40,"head offsets");
            Check(At("State")==296 && At("Info")==304 && At("TimeoutOrVersion")==816 && At("InfoTitle")==820 && At("InfoFlags")==948
                && At("Item")==952 && At("BalloonIcon")==968,"balloon offsets");
            var data=DesktopNotice.Data((IntPtr)0x1234,0x10,new string('x',400));
            Check(data.Size==976 && data.Icon==IntPtr.Zero && data.InfoFlags==0x11 && data.InfoTitle==DesktopNotice.Title,"filled for a balloon, silent, no icon load without NIF_ICON");
            // The marshaller writes the strings into the fixed buffers, cut with their final zero: no overflow, whatever the text.
            var buffer=Marshal.AllocHGlobal(976);
            try
            {
                Marshal.StructureToPtr(data,buffer,false);
                Check(Marshal.PtrToStringUni(buffer+304)!.Length==255 && Marshal.ReadInt16(buffer+304+255*2)==0,"Info cut at 255 characters plus zero");
                Check(Marshal.PtrToStringUni(buffer+820)==DesktopNotice.Title,"title in its place");
                Marshal.DestroyStructure<DesktopNotice.NotifyIconData>(buffer);
            }
            finally{Marshal.FreeHGlobal(buffer);}
        }),
        ("Desktop notice: the balloon's text, bounded, lines kept in order",() =>
        {
            Check(DesktopNotice.Body(["Alarm: Garden in 3 min"," Ventures ","  "])=="Alarm: Garden in 3 min\nVentures","lines joined, blanks left out");
            var many=Enumerable.Range(1,40).Select(n=>$"Alarm {n}: something rings now").ToArray();
            var body=DesktopNotice.Body(many);
            Check(body.Length<=DesktopNotice.BodyMax && body.EndsWith('…') && body.StartsWith("Alarm 1:"),"cut with an ellipsis: "+body.Length);
            Check(DesktopNotice.Body([])=="","nothing to show");
            Check(!new DesktopNotice().Show(IntPtr.Zero,["x"]),"no window: nothing shown, nothing called");
            new DesktopNotice().Remove();
        }),
        ("Desktop notice: the setting, on by default, read by its name, only with the app's window",() =>
        {
            var options=new AlarmSettings();
            Check(options.DesktopNotification && options.Copy().DesktopNotification,"on by default, kept by Copy");
            Check(Read("src/TenshoAssistant/MainWindow.DesktopData.cs").Contains("internal bool DesktopAlarmNotification()=>(configuration.AlarmOptions ?? new()).DesktopNotification;"),"the plugin's entry");
            Check(Read("src/TenshoAssistant.Desktop/Host.cs").Contains("GetMethod(\"DesktopAlarmNotification\",BindingFlags.Instance|BindingFlags.NonPublic,Type.EmptyTypes)"),"called by that name");
            var status=Read("src/TenshoAssistant.Desktop/StatusBar.cs");
            Check(status.Contains("if(Window!=IntPtr.Zero && host.AlarmNotification())notice.Show(Window,lines);"),"shown after the sound, with the window");
            var program=Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.IndexOf("status.Window=window!.Hwnd;",StringComparison.Ordinal)>program.IndexOf("if(headless)return Headless.Run(",StringComparison.Ordinal),"never in a headless run");
            Check(program.Contains("status.Close();\n        window.Dispose();"),"icon removed before the window closes");
            Check(Read("src/TenshoAssistant/MainWindow.Alarms.cs").Contains("BoolSetting(\"Alarms.DesktopNotification\",\"Alarms.DesktopNotificationTip\""),"its row in the alarm options");
        }),
    ];
}
