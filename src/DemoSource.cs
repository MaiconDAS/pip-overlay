namespace PipOverlay;

internal sealed class DemoSource : Form
{
    internal Color? TestColor {get;set;}
    internal void PauseAnimation(){timer.Stop();Invalidate();}
    readonly System.Windows.Forms.Timer timer=new() { Interval=10 };
    readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    internal DemoSource()
    {
        Text="PiP Overlay · fonte de teste";ClientSize=new Size(640,360);
        StartPosition=FormStartPosition.Manual;Location=new Point(40,80);TopMost=true;
        DoubleBuffered=true;BackColor=Color.FromArgb(20,30,45);
        timer.Tick+=(_,_)=>Invalidate();timer.Start();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        if(TestColor is Color testColor) {g.Clear(testColor);return;}
        using var background=new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle,Color.FromArgb(10,90,100),Color.FromArgb(60,25,100),30);
        g.FillRectangle(background,ClientRectangle);
        var t=clock.Elapsed.TotalSeconds;
        using var ball=new SolidBrush(Color.FromArgb(90,240,190));
        g.FillEllipse(ball,(float)((Math.Sin(t)+1)*Math.Max(0,Width-90)/2),Height/2-30,64,64);
        using var title=new Font("Segoe UI",24,FontStyle.Bold);using var sub=new Font("Segoe UI",14);
        g.DrawString("PiP Overlay",title,Brushes.White,24,22);
        g.DrawString("Vídeo de teste · movimento em tempo real",sub,Brushes.White,26,74);
        g.DrawString($"{clock.Elapsed:mm\\:ss\\.ff}     •     {DateTime.Now:HH:mm:ss}",sub,Brushes.White,26,ClientSize.Height-48);
    }
    protected override void Dispose(bool disposing) { if (disposing) timer.Dispose();base.Dispose(disposing); }
}
