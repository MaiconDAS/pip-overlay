using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PipOverlay;

internal static class Native
{
    internal const int GwlExStyle = -20;
    internal const long Topmost = 0x8, Transparent = 0x20, ToolWindow = 0x80,
        Layered = 0x80000, NoActivate = 0x8000000;
    internal const uint NoSize = 1, NoMove = 2, NoActivatePos = 0x10;
    internal static readonly nint HwndTopmost = new(-1), HwndNoTopmost = new(-2), HwndBottom = new(1);
    internal delegate bool EnumProc(nint hwnd, nint param);
    [StructLayout(LayoutKind.Sequential)] internal struct Rect
    {
        public int Left, Top, Right, Bottom;
        public Rect(Rectangle r) { Left = r.Left; Top = r.Top; Right = r.Right; Bottom = r.Bottom; }
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X,Y; public Point(int x,int y) { X=x; Y=y; } }
    [StructLayout(LayoutKind.Sequential)] internal struct Size { public int Width, Height; }
    [StructLayout(LayoutKind.Sequential)] internal struct ThumbnailProperties
    {
        public uint Flags;
        public Rect Destination, Source;
        public byte Opacity;
        [MarshalAs(UnmanagedType.Bool)] public bool Visible;
        [MarshalAs(UnmanagedType.Bool)] public bool ClientOnly;
    }
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback, nint param);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern int GetWindowText(nint hwnd,StringBuilder text,int count);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint hwnd,out uint processId);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] internal static extern nint GetWindowLongPtr(nint hwnd,int index);
    [DllImport("user32.dll", SetLastError=true)] internal static extern bool SetWindowPos(nint hwnd,nint after,int x,int y,int cx,int cy,uint flags);
    [DllImport("user32.dll", SetLastError=true)] internal static extern bool SetLayeredWindowAttributes(nint hwnd,uint key,byte alpha,uint flags);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern nint GetWindow(nint hwnd,uint command);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint hwnd,out Rect rect);
    [DllImport("user32.dll")] internal static extern nint WindowFromPoint(Point point);
    [DllImport("dwmapi.dll")] internal static extern int DwmRegisterThumbnail(nint destination,nint source,out nint thumbnail);
    [DllImport("dwmapi.dll")] internal static extern int DwmUnregisterThumbnail(nint thumbnail);
    [DllImport("dwmapi.dll")] internal static extern int DwmQueryThumbnailSourceSize(nint thumbnail,out Size size);
    [DllImport("dwmapi.dll")] internal static extern int DwmUpdateThumbnailProperties(nint thumbnail,ref ThumbnailProperties properties);
    [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(nint hwnd,int attribute,ref int value,int size);
    [DllImport("dwmapi.dll")] internal static extern int DwmFlush();
    internal static bool IsTopmost(nint hwnd) => (GetWindowLongPtr(hwnd,GwlExStyle).ToInt64() & Topmost) != 0;
    internal static uint ProcessId(nint hwnd) { GetWindowThreadProcessId(hwnd,out var pid); return pid; }
    internal static void Check(int hr) { if (hr < 0) Marshal.ThrowExceptionForHR(hr); }
}

internal sealed record WindowSource(nint Handle,uint ProcessId,string Browser,string Title,bool LikelyPip)
{
    public override string ToString() => $"{Browser}  |  {(LikelyPip ? "PiP · " : "")}{Title}";
    internal bool Exists => Native.IsWindow(Handle) && Native.ProcessId(Handle)==ProcessId;
    internal static List<WindowSource> Find()
    {
        var found = new List<WindowSource>();
        Native.EnumWindows((hwnd,_) => {
            if (!Native.IsWindowVisible(hwnd) || Native.IsIconic(hwnd)) return true;
            try {
                var pid=Native.ProcessId(hwnd);
                using var process=Process.GetProcessById((int)pid);
                var name=process.ProcessName;
                if (name != "msedge" && name != "chrome") return true;
                var text=new StringBuilder(1024); Native.GetWindowText(hwnd,text,text.Capacity);
                if (text.Length==0) return true;
                var title=text.ToString();
                Native.GetWindowRect(hwnd,out var rect);
                var pip=title.Contains("picture",StringComparison.OrdinalIgnoreCase) ||
                    title.Contains("imagem em imagem",StringComparison.OrdinalIgnoreCase) ||
                    (Native.IsTopmost(hwnd) && rect.Right-rect.Left<1280 && rect.Bottom-rect.Top<900);
                found.Add(new(hwnd,pid,name=="msedge" ? "Edge" : "Chrome",title,pip));
            } catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
            return true;
        },0);
        return found.OrderByDescending(x=>x.LikelyPip).ThenBy(x=>x.Browser).ThenBy(x=>x.Title).ToList();
    }
}

// Only transient Z-order is changed. No browser styles, settings, position or size are modified.
internal sealed class SourceLease : IDisposable
{
    readonly WindowSource source;
    readonly bool wasTopmost;
    bool disposed;
    internal SourceLease(WindowSource source)
    {
        this.source=source;
        if (!source.Exists) throw new InvalidOperationException("A janela selecionada foi fechada. Atualize a lista.");
        wasTopmost=Native.IsTopmost(source.Handle);
        try { KeepBehind(); } catch { Dispose(); throw; }
    }
    internal void KeepBehind()
    {
        if (disposed || !source.Exists) return;
        if (Native.IsTopmost(source.Handle)) {
            if (!Native.SetWindowPos(source.Handle,Native.HwndNoTopmost,0,0,0,0,Native.NoMove|Native.NoSize|Native.NoActivatePos))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),"Não foi possível colocar o PiP atrás do jogo.");
        }
        // Sending to bottom does not minimize/hide the source; its compositor surface remains available.
        if (!Native.SetWindowPos(source.Handle,Native.HwndBottom,0,0,0,0,Native.NoMove|Native.NoSize|Native.NoActivatePos))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed=true;
        if (wasTopmost && source.Exists)
            Native.SetWindowPos(source.Handle,Native.HwndTopmost,0,0,0,0,Native.NoMove|Native.NoSize|Native.NoActivatePos);
    }
}
