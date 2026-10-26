using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace PipOverlay;

internal sealed class RtssFrameOutput : IDisposable
{
    readonly RtssClient client=new();
    readonly string folder;
    readonly string[] paths;
    Bitmap? scaled;
    int index;
    internal long PublishedFrames {get;private set;}
    internal double LastPublishMilliseconds {get;private set;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern uint GetShortPathName(string path,StringBuilder result,uint size);
    internal RtssFrameOutput()
    {
        folder=Path.Combine(AppContext.BaseDirectory,"f-"+Guid.NewGuid().ToString("N")[..8]);
        try {
            Directory.CreateDirectory(folder);
            var shortName=new StringBuilder(1024);
            var length=GetShortPathName(folder,shortName,1024);
            string wireFolder=length>0 && length<1024?shortName.ToString():folder;
            paths=new[]{Path.Combine(wireFolder,"0.bmp"),Path.Combine(wireFolder,"1.bmp")};
            if(paths[0].Length>125) throw new IOException("Coloque a ferramenta em uma pasta de caminho mais curto para o RTSS carregar o vídeo.");
        }catch {client.Dispose();try{Directory.Delete(folder,false);}catch{}throw;}
    }
    internal static string Hypertext(string path,Rectangle target,Size pixels) =>
        $"<LI=\"{path}\"><FNT=Segoe UI,-16,400,1><P={target.X},{target.Y}><L0><E={target.Width},{target.Height}><I={target.Width},{target.Height},0,0,{pixels.Width},{pixels.Height}>";
    internal bool Publish(Bitmap frame,Settings settings,bool counter=false)
    {
        var clock=System.Diagnostics.Stopwatch.StartNew();
        var crop=settings.Crop(frame.Size);
        var size=Settings.Fit(crop.Size,new Rectangle(0,0,Math.Min(480,settings.Width),Math.Min(270,settings.Height))).Size;
        if(scaled==null || scaled.Size!=size) {scaled?.Dispose();scaled=new Bitmap(size.Width,size.Height,PixelFormat.Format24bppRgb);}
        using(var g=Graphics.FromImage(scaled)) {
            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            g.DrawImage(frame,new Rectangle(Point.Empty,size),crop,GraphicsUnit.Pixel);
            if(counter) {g.FillRectangle(Brushes.Black,0,0,180,28);g.DrawString($"Quadro {PublishedFrames}",SystemFonts.DefaultFont,Brushes.White,4,4);}
        }
        string path=paths[index],temporary=path+".tmp";
        // No PNG compression. Rename only after closing the completed bitmap.
        scaled.Save(temporary,ImageFormat.Bmp);File.Move(temporary,path,true);
        var target=Settings.Fit(crop.Size,new Rectangle(settings.X,settings.Y,settings.Width,settings.Height));
        bool published=client.Publish(Hypertext(path,target,size));
        if(published) {index=1-index;PublishedFrames++;}
        LastPublishMilliseconds=clock.Elapsed.TotalMilliseconds;return published;
    }
    public void Dispose()
    {
        try {client.Dispose();}finally {
            scaled?.Dispose();scaled=null;
            foreach(string name in new[]{"0.bmp","1.bmp","0.bmp.tmp","1.bmp.tmp"}) try {File.Delete(Path.Combine(folder,name));}catch(IOException){}catch(UnauthorizedAccessException){}
            try {Directory.Delete(folder,false);}catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }
}
