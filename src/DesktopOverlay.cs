using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PipOverlay;

internal sealed class DesktopOverlay : Form
{
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern int PipDesktopOpen(nint window);
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern int PipDesktopDraw();
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipDesktopStats([Out]ulong[] values);
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipDesktopResize();
    [DllImport("PipDesktop.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipDesktopClose();
    [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(nint window,uint color,byte alpha,uint flags);
    [DllImport("user32.dll")] static extern bool ShowWindow(nint window,int command);
    [DllImport("user32.dll")] static extern bool SetWindowPos(nint window,nint after,int x,int y,int width,int height,uint flags);
    delegate void ForegroundCallback(nint hook,uint kind,nint window,int objectId,int childId,uint thread,uint time);
    [DllImport("user32.dll")] static extern nint SetWinEventHook(uint first,uint last,nint module,ForegroundCallback callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(nint hook);
    [StructLayout(LayoutKind.Sequential)] struct WindowPosition {public nint Window,After;public int X,Y,Width,Height;public uint Flags;}
    readonly System.Windows.Forms.Timer present=new(),orderDelay=new(){Interval=100};
    volatile bool opened;
    readonly EventWaitHandle frameReady=new(false,EventResetMode.AutoReset,"Local\\PipDesktopFrame-"+Environment.ProcessId);
    RegisteredWaitHandle? frameWait;int queued;long draws,orderChecks;string? error;
    internal long DrawCalls=>draws;
    internal long OrderChecks=>orderChecks;
    internal string? Error=>error;
    internal int ErrorCode {get;private set;}
    readonly Queue<double> intervals=new();long lastPresented;double longestInterval;
    internal object Diagnostics(){var stats=PresentationStats();var sorted=intervals.Order().ToArray();return new{drawCalls=draws,zOrderChecks=orderChecks,presentAttempts=stats[0],acceptedPresents=stats[1],busyPresents=stats[2],presentErrorCode=stats[3],recentIntervalMedianMs=sorted.Length==0?0:sorted[sorted.Length/2],recentIntervalP95Ms=sorted.Length==0?0:sorted[Math.Min(sorted.Length-1,(int)(sorted.Length*.95))],longestIntervalMs=longestInterval,note="Accepted presentation submissions; intervals include source pauses and do not measure physical scanout or game FPS."};}
    internal ulong[] PresentationStats(){var result=new ulong[4];if(opened)PipDesktopStats(result);return result;}
    void RequestFrame(){
        if(!opened||IsDisposed||!IsHandleCreated||Interlocked.Exchange(ref queued,1)!=0)return;
        try{BeginInvoke(()=>{Interlocked.Exchange(ref queued,0);if(opened&&!IsDisposed)Render();});}catch(InvalidOperationException){Interlocked.Exchange(ref queued,0);}
    }
    void Render(){
        draws++;int result=PipDesktopDraw();
        if(result==1){long now=System.Diagnostics.Stopwatch.GetTimestamp();if(lastPresented!=0){double interval=System.Diagnostics.Stopwatch.GetElapsedTime(lastPresented,now).TotalMilliseconds;intervals.Enqueue(interval);if(intervals.Count>256)intervals.Dequeue();longestInterval=Math.Max(longestInterval,interval);}lastPresented=now;}
        if(result<0){ErrorCode=result;error=$"DirectComposition: 0x{result:X8}";present.Stop();return;}
        present.Interval=result==2?8:1000;
        if(Environment.TickCount64>=nextOrderCheck){nextOrderCheck=Environment.TickCount64+1000;EnsureTopmost();}
    }
    readonly ForegroundCallback foregroundCallback;
    nint foregroundHook,reorderHook;long nextOrderCheck;
    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80000|0x20|0x08000000|0x80|8;return p;}}
    internal DesktopOverlay(Settings settings,Rectangle monitor){
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;BackColor=Color.Black;AutoScaleMode=AutoScaleMode.None;SetStyle(ControlStyles.Selectable,false);
        foregroundCallback=(_,kind,window,objectId,_,_,_)=>{if(!opened||window==Handle||objectId!=0)return;if(kind==3)EnsureTopmost();else if(!orderDelay.Enabled)orderDelay.Start();};
        orderDelay.Tick+=(_,_)=>{orderDelay.Stop();EnsureTopmost();};
        Apply(settings,monitor);present.Interval=1000;present.Tick+=(_,_)=>{if(opened)Render();};
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);SetLayeredWindowAttributes(Handle,0,255,2);int hr=PipDesktopOpen(Handle);if(hr<0){PipDesktopClose();Native.Check(hr);}opened=true;foregroundHook=SetWinEventHook(3,3,0,foregroundCallback,0,0,0);reorderHook=SetWinEventHook(0x8004,0x8004,0,foregroundCallback,0,0,0);frameWait=ThreadPool.RegisterWaitForSingleObject(frameReady,(_,_)=>RequestFrame(),null,Timeout.Infinite,false);present.Start();}
    internal void ShowOverlay(){SetWindowPos(Handle,-1,0,0,0,0,0x13);ShowWindow(Handle,4);}
    void EnsureTopmost(){orderChecks++;if(opened&&Native.GetWindow(Handle,3)!=0)SetWindowPos(Handle,-1,0,0,0,0,0x13);}
    internal void Apply(Settings settings,Rectangle monitor){
        var next=settings.Bounds(monitor);if(Bounds==next&&Region!=null)return;Bounds=next;
        using var path=new GraphicsPath();const int diameter=12;
        path.AddArc(0,0,diameter,diameter,180,90);path.AddArc(Width-diameter,0,diameter,diameter,270,90);path.AddArc(Width-diameter,Height-diameter,diameter,diameter,0,90);path.AddArc(0,Height-diameter,diameter,diameter,90,90);path.CloseFigure();
        var previous=Region;Region=new Region(path);previous?.Dispose();
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);if(opened){PipDesktopResize();RequestFrame();}}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x46&&m.LParam!=0){var p=Marshal.PtrToStructure<WindowPosition>(m.LParam);p.Flags|=0x10;Marshal.StructureToPtr(p,m.LParam,false);}
        if(m.Msg==0x84){m.Result=-1;return;}if(m.Msg==0x21){m.Result=3;return;}base.WndProc(ref m);
    }
    protected override void OnHandleDestroyed(EventArgs e){frameWait?.Unregister(null);frameWait=null;present.Stop();orderDelay.Stop();if(foregroundHook!=0){UnhookWinEvent(foregroundHook);foregroundHook=0;}if(reorderHook!=0){UnhookWinEvent(reorderHook);reorderHook=0;}if(opened){PipDesktopClose();opened=false;}GC.KeepAlive(foregroundCallback);base.OnHandleDestroyed(e);}
    protected override void Dispose(bool disposing){if(disposing){frameWait?.Unregister(null);frameWait=null;present.Dispose();orderDelay.Dispose();}base.Dispose(disposing);if(disposing)frameReady.Dispose();}
}
