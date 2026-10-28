using System.Drawing.Drawing2D;
namespace PipOverlay;
internal static class UiTheme
{
    internal static Bitmap Brand(int size){using var stream=typeof(UiTheme).Assembly.GetManifestResourceStream("PipOverlay.Brand.ico")!;using var icon=new Icon(stream,new Size(size,size));return icon.ToBitmap();}
    internal static readonly Color Accent=Color.FromArgb(108,231,196),Panel=Color.FromArgb(25,33,46),Muted=Color.FromArgb(154,169,190);
    internal static Bitmap Icon(string kind,Color color,int size=22){
        var image=new Bitmap(size,size);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/24f,size/24f);using var p=new Pen(color,1.8f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};using var b=new SolidBrush(color);
        if(kind=="keyboard"){g.DrawRectangle(p,2,5,20,14);for(int y=8;y<=12;y+=4)for(int x=5;x<=19;x+=4)g.FillRectangle(b,x,y,1.5f,1.5f);g.DrawLine(p,8,16,16,16);}
        else if(kind=="play")g.FillPolygon(b,new[]{new PointF(8,5),new PointF(19,12),new PointF(8,19)});
        else if(kind=="stop")g.FillRectangle(b,6,6,12,12);
        else if(kind=="refresh"){g.DrawArc(p,4,4,16,16,40,285);g.DrawLines(p,new[]{new PointF(20,3),new PointF(20,9),new PointF(14,9)});}
        else {g.DrawRectangle(p,2,4,20,15);g.FillRectangle(b,12,12,9,6);}
        return image;
    }
    internal static void Combo(ComboBox box){box.FlatStyle=FlatStyle.Flat;box.BackColor=Panel;box.ForeColor=Color.White;box.DrawMode=DrawMode.OwnerDrawFixed;box.ItemHeight=28;box.Margin=new Padding(0,0,0,0);box.DrawItem+=(_,e)=>{using var fill=new SolidBrush((e.State&DrawItemState.Selected)!=0?Color.FromArgb(43,65,77):Panel);e.Graphics.FillRectangle(fill,e.Bounds);if(e.Index>=0)TextRenderer.DrawText(e.Graphics,box.Items[e.Index]?.ToString(),box.Font,Rectangle.Inflate(e.Bounds,-9,0),Color.White,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};}
    internal static Control Field(string name,Control control,int width){var panel=new TableLayoutPanel{Width=width,Height=66,ColumnCount=1,RowCount=2,Margin=new Padding(0,0,16,0)};panel.RowStyles.Add(new(SizeType.Absolute,25));panel.RowStyles.Add(new(SizeType.Percent,100));panel.Controls.Add(new Label{Text=name,ForeColor=Muted,AutoSize=true,Margin=Padding.Empty});control.Dock=DockStyle.Fill;panel.Controls.Add(control);return panel;}
    internal static void Button(Button button,string text,string icon,bool primary=false){button.Text=text;button.AutoSize=false;button.Size=new Size(primary?174:126,42);button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=primary?0:1;button.FlatAppearance.BorderColor=Color.FromArgb(55,72,88);button.BackColor=primary?Accent:Panel;button.ForeColor=primary?Color.FromArgb(10,33,30):Color.White;button.Image=Icon(icon,button.ForeColor);button.ImageAlign=ContentAlignment.MiddleLeft;button.TextImageRelation=TextImageRelation.ImageBeforeText;button.Padding=new Padding(10,0,8,0);button.Cursor=Cursors.Hand;button.Margin=new Padding(0,0,10,0);}
}
