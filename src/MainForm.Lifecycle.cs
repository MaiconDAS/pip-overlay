namespace PipOverlay;

internal sealed class CaptureAvailability
{
    internal bool Suspended {get;set;}
    internal bool Locked {get;set;}
    internal bool CanRun=>!Suspended&&!Locked;
    internal static bool IsTransient(int hr)=>unchecked((uint)hr) is 0x887A0005 or 0x887A0006 or 0x887A0007 or 0x887A0020;
}
internal sealed partial class MainForm
{
    void ConfigureDiscovery(){
        if(designPreviewMode||IsDisposed||!IsHandleCreated)return;
        if(!availability.CanRun||(!automatic.Enabled&&!Visible)){discovery.Stop();windowEvents?.Dispose();windowEvents=null;return;}
        windowEvents??=new WindowEvents(()=>{if(!settings.PreferDesktop)GameDiscovery.Invalidate();Discover();});
        discovery.Interval=Visible?5000:15000;discovery.Start();
    }
    void QueueAvailability(Action change){
        if(IsDisposed||!IsHandleCreated)return;
        try{BeginInvoke(()=>{
            if(IsDisposed)return;bool previous=availability.CanRun;change();ConfigureDiscovery();
            if(!availability.CanRun){Stop(L.T("Captura suspensa enquanto a sessão está bloqueada ou o computador está suspenso."));return;}
            if(!previous){failedTargets.Clear();RebuildMonitors();RestoreMonitorLayout();UpdateCanvas();automatic.Reset();Discover();}
        });}catch(InvalidOperationException){}
    }
    void PowerChanged(object sender,Microsoft.Win32.PowerModeChangedEventArgs e){
        if(e.Mode==Microsoft.Win32.PowerModes.Suspend)QueueAvailability(()=>availability.Suspended=true);
        else if(e.Mode==Microsoft.Win32.PowerModes.Resume)QueueAvailability(()=>availability.Suspended=false);
    }
    void SessionChanged(object sender,Microsoft.Win32.SessionSwitchEventArgs e){
        if(e.Reason is Microsoft.Win32.SessionSwitchReason.SessionLock or Microsoft.Win32.SessionSwitchReason.RemoteDisconnect or Microsoft.Win32.SessionSwitchReason.ConsoleDisconnect)QueueAvailability(()=>availability.Locked=true);
        else if(e.Reason is Microsoft.Win32.SessionSwitchReason.SessionUnlock or Microsoft.Win32.SessionSwitchReason.RemoteConnect or Microsoft.Win32.SessionSwitchReason.ConsoleConnect)QueueAvailability(()=>availability.Locked=false);
    }
}
