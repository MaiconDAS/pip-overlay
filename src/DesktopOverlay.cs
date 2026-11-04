using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PipOverlay;

internal sealed class DesktopOverlay : Form
{
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern int PipDesktopOpen(nint window);
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipDesktopDraw();
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipDesktopResize();
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipDesktopClose();
    [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(nint window,uint color,byte alpha,uint flags);
    [DllImport("user32.dll")] static extern bool ShowWindow(nint window,int command);
    [DllImport("user32.dll")] static extern bool SetWindowPos(nint window,nint after,int x,int y,int width,int height,uint flags);
    delegate void ForegroundCallback(nint hook,uint kind,nint window,int objectId,int childId,uint thread,uint time);
    [DllImport("user32.dll")] static extern nint SetWinEventHook(uint first,uint last,nint module,ForegroundCallback callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(nint hook);
    [StructLayout(LayoutKind.Sequential)] struct WindowPosition {public nint Window,After;public int X,Y,Width,Height;public uint Flags;}
    readonly System.Windows.Forms.Timer present=new();
    bool opened;
    readonly ForegroundCallback foregroundCallback;
    nint foregroundHook;long nextOrderCheck;
    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80000|0x20|0x08000000|0x80|8;return p;}}
    internal DesktopOverlay(Settings settings,Rectangle monitor){
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;BackColor=Color.Black;AutoScaleMode=AutoScaleMode.None;SetStyle(ControlStyles.Selectable,false);
        foregroundCallback=(_,_,window,_,_,_,_)=>{if(opened&&window!=Handle)EnsureTopmost();};
        Apply(settings,monitor);present.Tick+=(_,_)=>{if(opened){PipDesktopDraw();if(Environment.TickCount64>=nextOrderCheck){nextOrderCheck=Environment.TickCount64+250;EnsureTopmost();}}};
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);SetLayeredWindowAttributes(Handle,0,255,2);int hr=PipDesktopOpen(Handle);if(hr<0){PipDesktopClose();Native.Check(hr);}opened=true;foregroundHook=SetWinEventHook(3,3,0,foregroundCallback,0,0,0);present.Start();}
    internal void ShowOverlay(){SetWindowPos(Handle,-1,0,0,0,0,0x13);ShowWindow(Handle,4);}
    void EnsureTopmost(){if(opened&&Native.GetWindow(Handle,3)!=0)SetWindowPos(Handle,-1,0,0,0,0,0x13);}
    internal void Apply(Settings settings,Rectangle monitor){
        Bounds=settings.Bounds(monitor);present.Interval=8; // Native renderer presents only newly captured frames.
        using var path=new GraphicsPath();const int diameter=12;
        path.AddArc(0,0,diameter,diameter,180,90);path.AddArc(Width-diameter,0,diameter,diameter,270,90);path.AddArc(Width-diameter,Height-diameter,diameter,diameter,0,90);path.AddArc(0,Height-diameter,diameter,diameter,90,90);path.CloseFigure();
        var previous=Region;Region=new Region(path);previous?.Dispose();
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);if(opened)PipDesktopResize();}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x46&&m.LParam!=0){var p=Marshal.PtrToStructure<WindowPosition>(m.LParam);p.Flags|=0x10;Marshal.StructureToPtr(p,m.LParam,false);}
        if(m.Msg==0x84){m.Result=-1;return;}if(m.Msg==0x21){m.Result=3;return;}base.WndProc(ref m);
    }
    protected override void OnHandleDestroyed(EventArgs e){present.Stop();if(foregroundHook!=0){UnhookWinEvent(foregroundHook);foregroundHook=0;}if(opened){PipDesktopClose();opened=false;}GC.KeepAlive(foregroundCallback);base.OnHandleDestroyed(e);}
    protected override void Dispose(bool disposing){if(disposing)present.Dispose();base.Dispose(disposing);}
}
