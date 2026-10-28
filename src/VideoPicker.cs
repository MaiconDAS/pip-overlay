namespace PipOverlay;
internal sealed class VideoPicker : Form
{
    readonly List<(nint Handle,Panel Card)> thumbnails=new();
    readonly System.Windows.Forms.Timer refresh=new(){Interval=300};
    internal int ThumbnailCount=>thumbnails.Count;
    internal WindowSource? Selected {get;private set;}
    internal VideoPicker(IReadOnlyList<WindowSource> sources){
        Text="Escolher vídeo";ClientSize=new Size(680,470);MinimumSize=new Size(500,360);StartPosition=FormStartPosition.CenterParent;
        BackColor=Color.FromArgb(15,20,30);ForeColor=Color.White;Font=new Font("Segoe UI",10);
        var grid=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(16)};Controls.Add(grid);
        foreach(var source in sources){
            var card=new Panel{Size=new Size(300,200),Margin=new Padding(0,0,16,16),BackColor=UiTheme.Panel,Cursor=Cursors.Hand,AccessibleName=source.Browser+" · escolher vídeo"};
            var choose=new Button{Text=source.Browser+" · Escolher",Dock=DockStyle.Bottom,Height=38,FlatStyle=FlatStyle.Flat,ForeColor=Color.White};card.Controls.Add(choose);grid.Controls.Add(card);
            void SelectVideo(){if(!source.Exists){choose.Text="Vídeo fechado";choose.Enabled=false;return;}Selected=source;DialogResult=DialogResult.OK;Close();}
            card.Click+=(_,_)=>SelectVideo();choose.Click+=(_,_)=>SelectVideo();
            Shown+=(_,_)=>{if(Native.DwmRegisterThumbnail(Handle,source.Handle,out var thumbnail)>=0)thumbnails.Add((thumbnail,card));else choose.Text=source.Browser+" · Sem prévia · Escolher";};
        }
        void Draw(){foreach(var item in thumbnails){var point=PointToClient(item.Card.PointToScreen(Point.Empty));var rect=new Rectangle(point.X+6,point.Y+6,item.Card.Width-12,item.Card.Height-50);
            if(Native.DwmQueryThumbnailSourceSize(item.Handle,out var size)>=0&&size.Width>0&&size.Height>0)rect=Settings.Fit(new Size(size.Width,size.Height),rect);
            var viewport=ClientRectangle;bool visible=viewport.Contains(rect);var p=new Native.ThumbnailProperties{Flags=1|4|8|16,Destination=new Native.Rect(rect),Visible=visible,Opacity=255,ClientOnly=true};Native.DwmUpdateThumbnailProperties(item.Handle,ref p);}}
        Shown+=(_,_)=>{Draw();refresh.Start();};refresh.Tick+=(_,_)=>Draw();
    }
    protected override void Dispose(bool disposing){if(disposing){refresh.Dispose();foreach(var item in thumbnails)Native.DwmUnregisterThumbnail(item.Handle);thumbnails.Clear();}base.Dispose(disposing);}
}
