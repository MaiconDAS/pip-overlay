using System.Runtime.InteropServices;
namespace PipOverlay;

// Out-of-context notifications only; no code is loaded into observed applications.
internal sealed class WindowEvents : IDisposable
{
    delegate void Callback(nint hook,uint kind,nint window,int objectId,int childId,uint thread,uint time);
    [DllImport("user32.dll")] static extern nint SetWinEventHook(uint first,uint last,nint module,Callback callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(nint hook);
    readonly Callback callback;
    readonly nint foreground,windows;
    readonly System.Windows.Forms.Timer debounce=new(){Interval=400};
    bool disposed;
    internal WindowEvents(Action changed){
        callback=(_,_,window,objectId,_,_,_)=>{if(disposed||window==0||objectId!=0)return;debounce.Stop();debounce.Start();};
        debounce.Tick+=(_,_)=>{debounce.Stop();if(!disposed)changed();};
        // SKIPOWNPROCESS prevents editor/preview windows from triggering discovery.
        foreground=SetWinEventHook(3,3,0,callback,0,0,2);
        windows=SetWinEventHook(0x8000,0x8003,0,callback,0,0,2);
    }
    public void Dispose(){if(disposed)return;disposed=true;if(foreground!=0)UnhookWinEvent(foreground);if(windows!=0)UnhookWinEvent(windows);debounce.Dispose();GC.KeepAlive(callback);}
}
