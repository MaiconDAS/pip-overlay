using System.Drawing.Imaging;

namespace PipOverlay;

// Experimental file-backed transport: bounded to two PNG files, not a video recording.
internal sealed class LegacyRtssFrameOutput : IDisposable
{
    readonly RtssClient client=new();
    readonly string folder;
    int index;
    internal long PublishedFrames {get;private set;}
    internal LegacyRtssFrameOutput()
    {
        folder=Path.Combine(AppContext.BaseDirectory,"f-"+Guid.NewGuid().ToString("N")[..8]);
        try {Directory.CreateDirectory(folder);}catch {client.Dispose();throw;}
    }
    internal bool Publish(Bitmap frame,Settings settings,bool counter=false)
    {
        var crop=settings.Crop(frame.Size);
        var size=Settings.Fit(crop.Size,new Rectangle(0,0,Math.Min(480,settings.Width),Math.Min(270,settings.Height))).Size;
        using var scaled=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(scaled)) {
            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            using var attributes=new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix {Matrix33=settings.Opacity/100f});
            g.DrawImage(frame,new Rectangle(Point.Empty,size),crop.X,crop.Y,crop.Width,crop.Height,GraphicsUnit.Pixel,attributes);
            if(counter) {g.FillRectangle(Brushes.Black,0,0,180,28);g.DrawString($"Quadro {PublishedFrames}",SystemFonts.DefaultFont,Brushes.White,4,4);}
        }
        string path=Path.Combine(folder,$"frame-{index}.png");
        string temporary=path+".tmp"; scaled.Save(temporary,ImageFormat.Png); File.Move(temporary,path,true);
        var target=Settings.Fit(crop.Size,new Rectangle(settings.X,settings.Y,settings.Width,settings.Height));
        string text=$"<LI=\"{path}\"><P={target.X},{target.Y}><I={target.Width},{target.Height},0,0,{size.Width},{size.Height}>";
        if(!client.Publish(text)) return false;
        index=1-index;PublishedFrames++;return true;
    }
    public void Dispose()
    {
        try {client.Dispose();}finally {
            // Exact files created by this instance only. No recursive directory removal.
            foreach(string name in new[]{"frame-0.png","frame-1.png","frame-0.png.tmp","frame-1.png.tmp"}) try {File.Delete(Path.Combine(folder,name));}catch(IOException){}catch(UnauthorizedAccessException){}
            try {Directory.Delete(folder,false);}catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }
}
