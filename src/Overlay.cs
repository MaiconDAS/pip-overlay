namespace PipOverlay;

internal sealed class Overlay : Form
{
    WindowCapture? capture;
    readonly bool useRtss;
    RtssFrameOutput? rtss;
    string? outputError;
    Bitmap? frame;
    Settings settings=new();
    readonly System.Windows.Forms.Timer render=new() {Interval=33};
    readonly System.Diagnostics.Stopwatch elapsed=new();
    internal string? CaptureError => outputError ?? capture?.Error;
    internal long CapturedFrames => capture?.Frames ?? 0;
    internal Color? CenterPixel => frame?.GetPixel(frame.Width/2,frame.Height/2);
    internal bool WaitingTooLong => CapturedFrames==0 && elapsed.Elapsed.TotalSeconds>8;
    internal Overlay(bool useRtss=false)
    {
        this.useRtss=useRtss;
        Text="PiP Overlay · vídeo";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
        StartPosition=FormStartPosition.Manual;BackColor=Color.Black;AutoScaleMode=AutoScaleMode.None;
        DoubleBuffered=true;SetStyle(ControlStyles.Selectable,false);
        if(useRtss) render.Interval=67;
        render.Tick+=(_,_)=> {
            var next=capture?.TakeFrame();if(next==null) return;frame?.Dispose();frame=next;
            if(rtss!=null) {try {rtss.Publish(frame,settings);}catch(Exception e) {outputError=e.Message;render.Stop();}}
            else Invalidate();
        };
    }
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams {
        get {var cp=base.CreateParams;cp.ExStyle|=(int)(Native.Layered|Native.Transparent|Native.NoActivate|Native.ToolWindow|Native.Topmost);return cp;}
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);Native.SetLayeredWindowAttributes(Handle,0,255,2);
        int noRound=1;Native.DwmSetWindowAttribute(Handle,33,ref noRound,4);
    }
    internal void Start(WindowSource source,Settings settings,Rectangle monitor)
    {
        if(useRtss) rtss=new RtssFrameOutput();
        else _=Handle;
        capture=new WindowCapture(source.Handle);Apply(settings,monitor);elapsed.Restart();
        if(!useRtss) Show();
        render.Start();
    }
    internal Size SourceSize => capture?.SourceSize ?? Size.Empty;
    internal void Apply(Settings settings,Rectangle monitor)
    {
        this.settings=settings.Validated();var box=this.settings.Bounds(monitor);
        if(useRtss) {
            this.settings=this.settings with {X=box.X-monitor.X,Y=box.Y-monitor.Y,Width=box.Width,Height=box.Height};
            return;
        }
        if(!Native.SetWindowPos(Handle,Native.HwndTopmost,box.X,box.Y,box.Width,box.Height,Native.NoActivatePos))
            throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
        if(!Native.SetLayeredWindowAttributes(Handle,0,(byte)(255*this.settings.Opacity/100),2))
            throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if(frame==null) {TextRenderer.DrawText(e.Graphics,"Aguardando imagem…",Font,ClientRectangle,Color.LightGray,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
        var crop=settings.Crop(frame.Size);var target=Settings.Fit(crop.Size,ClientRectangle);
        e.Graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
        e.Graphics.DrawImage(frame,target,crop,GraphicsUnit.Pixel);
    }
    protected override void WndProc(ref Message m) {if(m.Msg==0x21) {m.Result=3;return;}base.WndProc(ref m);}
    protected override void Dispose(bool disposing)
    {
        if(disposing) {render.Stop();render.Dispose();capture?.Dispose();capture=null;frame?.Dispose();frame=null;rtss?.Dispose();rtss=null;}
        base.Dispose(disposing);
    }
}
