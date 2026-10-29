using System.Drawing.Drawing2D;
namespace PipOverlay;

// Keep standard WinForms input, keyboard and accessibility behavior; only paint changes.
internal sealed class ModernComboBox : ComboBox
{
    public ModernComboBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;}
    protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(UiTheme.Panel);TextRenderer.DrawText(g,Text,Font,new Rectangle(12,0,Math.Max(0,Width-42),Height),Enabled?Color.White:UiTheme.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);using var pen=new Pen(Enabled?UiTheme.Muted:Color.FromArgb(72,84,103),1.5f);g.SmoothingMode=SmoothingMode.AntiAlias;g.DrawLines(pen,new[]{new Point(Width-21,Height/2-2),new Point(Width-16,Height/2+3),new Point(Width-11,Height/2-2)});using var border=new Pen(Focused?UiTheme.Accent:Color.FromArgb(49,63,81));g.DrawRectangle(border,0,0,Width-1,Height-1);}
}

internal sealed class ModernToggle : CheckBox
{
    public ModernToggle(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);AutoSize=false;Height=40;Cursor=Cursors.Hand;}
    public override Size GetPreferredSize(Size proposedSize)=>new(Math.Max(240,TextRenderer.MeasureText(Text,Font).Width+62),40);
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var track=new Rectangle(Width-43,(Height-22)/2,42,22);using var path=new GraphicsPath();path.AddArc(track.X,track.Y,22,22,90,180);path.AddArc(track.Right-22,track.Y,22,22,270,180);path.CloseFigure();using var fill=new SolidBrush(Checked?UiTheme.Accent:Color.FromArgb(63,77,96));e.Graphics.FillPath(fill,path);using var knob=new SolidBrush(Checked?Color.FromArgb(15,40,34):Color.FromArgb(219,227,238));e.Graphics.FillEllipse(knob,track.X+(Checked?23:3),track.Y+3,16,16);TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(0,0,Width-54,Height),Enabled?ForeColor:UiTheme.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);}
}

internal sealed class ModernSlider : TrackBar
{
    public ModernSlider(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);TickStyle=TickStyle.None;Cursor=Cursors.Hand;AccessibleName="Margem de encaixe";}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x201||(m.Msg==0x200&&Capture)){if(m.Msg==0x201){Focus();Capture=true;}int x=unchecked((short)(long)m.LParam);Value=Math.Clamp(Minimum+(int)Math.Round((x-12d)/Math.Max(1,Width-24)*(Maximum-Minimum)),Minimum,Maximum);return;}
        if(m.Msg==0x202&&Capture){Capture=false;return;}base.WndProc(ref m);
    }
    protected override void OnValueChanged(EventArgs e){base.OnValueChanged(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;int y=Height/2;float x=12+(Width-24)*(Value-Minimum)/(float)Math.Max(1,Maximum-Minimum);using var rail=new Pen(Color.FromArgb(49,63,81),4){StartCap=LineCap.Round,EndCap=LineCap.Round};using var active=new Pen(UiTheme.Accent,4){StartCap=LineCap.Round,EndCap=LineCap.Round};e.Graphics.DrawLine(rail,12,y,Width-12,y);e.Graphics.DrawLine(active,12,y,x,y);using var knob=new SolidBrush(UiTheme.Accent);e.Graphics.FillEllipse(knob,x-7,y-7,14,14);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);}
}

internal sealed class ModernButton : Button
{
    bool hover;
    public ModernButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent?.BackColor??BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new Rectangle(1,1,Width-3,Height-3);using var path=new GraphicsPath();const int d=14;path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();var color=Enabled&&hover?ControlPaint.Light(BackColor,.12f):BackColor;using var fill=new SolidBrush(color);e.Graphics.FillPath(fill,path);if(FlatAppearance.BorderSize>0){using var pen=new Pen(Focused?UiTheme.Accent:FlatAppearance.BorderColor);e.Graphics.DrawPath(pen,path);}int left=8;if(Image!=null){e.Graphics.DrawImage(Image,12,(Height-Image.Height)/2,Image.Width,Image.Height);left=Image.Width+20;}TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(left,0,Width-left-8,Height),Enabled?ForeColor:UiTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(r,-4,-4));}
}
