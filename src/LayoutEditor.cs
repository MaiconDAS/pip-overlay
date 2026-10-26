namespace PipOverlay;

[Flags] internal enum ResizeEdge {None=0,Left=1,Right=2,Top=4,Bottom=8}
internal static class LayoutMath
{
    internal static Rectangle Constrain(Rectangle box,Size screen,double aspect)
    {
        aspect=Math.Clamp(aspect,0.05,20);
        int width=Math.Clamp(box.Width,Math.Min(120,screen.Width),screen.Width);
        int height=Math.Max(1,(int)Math.Round(width/aspect));
        if(height>screen.Height){height=screen.Height;width=Math.Max(1,(int)Math.Round(height*aspect));}
        return new(Math.Clamp(box.X,0,screen.Width-width),Math.Clamp(box.Y,0,screen.Height-height),width,height);
    }
    internal static Rectangle Drag(Rectangle start,Point delta,Size screen)=>new(
        Math.Clamp(start.X+delta.X,0,Math.Max(0,screen.Width-start.Width)),Math.Clamp(start.Y+delta.Y,0,Math.Max(0,screen.Height-start.Height)),start.Width,start.Height);
    internal static Rectangle Resize(Rectangle start,Point delta,ResizeEdge edge,Size screen,double aspect)
    {
        bool left=edge.HasFlag(ResizeEdge.Left),right=edge.HasFlag(ResizeEdge.Right),top=edge.HasFlag(ResizeEdge.Top),bottom=edge.HasFlag(ResizeEdge.Bottom);
        double width=start.Width+(left?-delta.X:right?delta.X:0);
        double vertical=(start.Height+(top?-delta.Y:bottom?delta.Y:0))*aspect;
        if(!(left||right) || ((top||bottom)&&Math.Abs(vertical-start.Width)>Math.Abs(width-start.Width))) width=vertical;
        double ax=left?start.Right:right?start.Left:start.Left+start.Width/2.0;
        double ay=top?start.Bottom:bottom?start.Top:start.Top+start.Height/2.0;
        double maxW=left?ax:right?screen.Width-ax:2*Math.Min(ax,screen.Width-ax);
        double maxH=top?ay:bottom?screen.Height-ay:2*Math.Min(ay,screen.Height-ay);
        double limit=Math.Max(1,Math.Min(maxW,maxH*aspect));
        width=Math.Clamp(width,Math.Min(120,limit),limit);
        int w=Math.Max(1,(int)Math.Floor(width)),h=Math.Max(1,(int)Math.Round(w/aspect));
        int x=(int)Math.Round(left?ax-w:right?ax:ax-w/2.0),y=(int)Math.Round(top?ay-h:bottom?ay:ay-h/2.0);
        return new(Math.Clamp(x,0,Math.Max(0,screen.Width-w)),Math.Clamp(y,0,Math.Max(0,screen.Height-h)),w,h);
    }
}
internal sealed class LayoutEditor : Control
{
    internal event Action<Rectangle>? LayoutChanged;
    internal Size MonitorSize {get;set;}=new(1920,1080);
    internal double Aspect {get;set;}=16.0/9;
    internal Rectangle Video {get;set;}=new(24,24,420,236);
    Bitmap? preview;
    nint thumbnail;
    internal void AttachSource(nint source){DetachSource();var form=FindForm();if(form!=null&&Native.DwmRegisterThumbnail(form.Handle,source,out thumbnail)>=0)RefreshThumbnail();}
    internal void DetachSource(){if(thumbnail!=0){Native.DwmUnregisterThumbnail(thumbnail);thumbnail=0;}Invalidate();}
    internal void RefreshThumbnail(){if(thumbnail==0)return;var form=FindForm();if(form==null)return;var box=ToPreview(Video);box.Inflate(-3,-3);box.Location=form.PointToClient(PointToScreen(box.Location));var p=new Native.ThumbnailProperties{Flags=1|4|8|16,Destination=new Native.Rect(box),Opacity=255,Visible=Visible&&form.WindowState!=FormWindowState.Minimized,ClientOnly=true};Native.DwmUpdateThumbnailProperties(thumbnail,ref p);}
    bool dragging;
    ResizeEdge edge;
    Rectangle original;
    Point anchor;
    internal LayoutEditor(){DoubleBuffered=true;BackColor=Color.FromArgb(18,24,34);Cursor=Cursors.Default;AccessibleName="Editor visual: arraste o vídeo ou suas bordas";}
    Rectangle Canvas=>Settings.Fit(MonitorSize,new Rectangle(20,20,Math.Max(1,Width-40),Math.Max(1,Height-40)));
    internal Rectangle ToPreview(Rectangle box) {
        var canvas=Canvas;double s=(double)canvas.Width/MonitorSize.Width;
        return new(canvas.X+(int)Math.Round(box.X*s),canvas.Y+(int)Math.Round(box.Y*s),Math.Max(1,(int)Math.Round(box.Width*s)),Math.Max(1,(int)Math.Round(box.Height*s)));
    }
    internal void SetPreview(Bitmap? image){if(image==null)return;preview?.Dispose();preview=image;Invalidate();}
    ResizeEdge Hit(Point p) {
        var b=ToPreview(Video);var expanded=b;expanded.Inflate(7,7);if(!expanded.Contains(p))return ResizeEdge.None;
        ResizeEdge result=ResizeEdge.None;
        if(Math.Abs(p.X-b.Left)<=7)result|=ResizeEdge.Left;else if(Math.Abs(p.X-b.Right)<=7)result|=ResizeEdge.Right;
        if(Math.Abs(p.Y-b.Top)<=7)result|=ResizeEdge.Top;else if(Math.Abs(p.Y-b.Bottom)<=7)result|=ResizeEdge.Bottom;
        return result;
    }
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;
        edge=Hit(e.Location);if(edge==ResizeEdge.None&&!ToPreview(Video).Contains(e.Location))return;
        anchor=e.Location;original=Video;dragging=true;Capture=true;
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);
        if(dragging) {
            double scale=(double)MonitorSize.Width/Canvas.Width;
            var delta=new Point((int)Math.Round((e.X-anchor.X)*scale),(int)Math.Round((e.Y-anchor.Y)*scale));
            Video=edge==ResizeEdge.None?LayoutMath.Drag(original,delta,MonitorSize):LayoutMath.Resize(original,delta,edge,MonitorSize,Aspect);
            Invalidate();LayoutChanged?.Invoke(Video);return;
        }
        var hit=Hit(e.Location);
        Cursor=hit==ResizeEdge.None?(ToPreview(Video).Contains(e.Location)?Cursors.SizeAll:Cursors.Default):
            hit is ResizeEdge.Left or ResizeEdge.Right?Cursors.SizeWE:hit is ResizeEdge.Top or ResizeEdge.Bottom?Cursors.SizeNS:
            hit==(ResizeEdge.Left|ResizeEdge.Top)||hit==(ResizeEdge.Right|ResizeEdge.Bottom)?Cursors.SizeNWSE:Cursors.SizeNESW;
    }
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Width<50||Height<50)return;
        var c=Canvas;using var bg=new SolidBrush(Color.FromArgb(9,13,20));e.Graphics.FillRectangle(bg,c);
        using var grid=new Pen(Color.FromArgb(34,44,58));for(int i=1;i<4;i++){e.Graphics.DrawLine(grid,c.Left+c.Width*i/4,c.Top,c.Left+c.Width*i/4,c.Bottom);e.Graphics.DrawLine(grid,c.Left,c.Top+c.Height*i/4,c.Right,c.Top+c.Height*i/4);}e.Graphics.DrawRectangle(grid,c);
        RefreshThumbnail();var box=ToPreview(Video);if(preview!=null)e.Graphics.DrawImage(preview,box);else {using var fill=new SolidBrush(Color.FromArgb(30,70,77));e.Graphics.FillRectangle(fill,box);TextRenderer.DrawText(e.Graphics,"PiP",Font,box,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
        using var pen=new Pen(Color.FromArgb(79,220,182),2);e.Graphics.DrawRectangle(pen,box);
        foreach(var p in new[]{new Point(box.Left,box.Top),new Point(box.Right,box.Top),new Point(box.Left,box.Bottom),new Point(box.Right,box.Bottom),new Point(box.Left+box.Width/2,box.Top),new Point(box.Left+box.Width/2,box.Bottom),new Point(box.Left,box.Top+box.Height/2),new Point(box.Right,box.Top+box.Height/2)})e.Graphics.FillRectangle(Brushes.White,p.X-3,p.Y-3,6,6);
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);Invalidate();}
    protected override void Dispose(bool disposing){if(disposing){DetachSource();preview?.Dispose();}base.Dispose(disposing);}
}
