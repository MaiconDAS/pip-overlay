namespace PipOverlay;
internal static class GeometryTests
{
    internal static int Run(string path){
        var results=new List<string>();int failed=0;
        void Check(string name,bool ok){results.Add($"{(ok?"PASS":"FAIL")} {name}");if(!ok)failed++;}
        var screen=new Size(1920,1080);var box=new Rectangle(200,100,640,360);
        Check("Arraste conserva tamanho",LayoutMath.Drag(box,new(40,20),screen)==new Rectangle(240,120,640,360));
        Check("Arraste limita origem",LayoutMath.Drag(box,new(-9999,-9999),screen)==new Rectangle(0,0,640,360));
        Check("Arraste limita canto final",LayoutMath.Drag(box,new(9999,9999),screen)==new Rectangle(1280,720,640,360));
        foreach(var edge in new[]{ResizeEdge.Left,ResizeEdge.Right,ResizeEdge.Top,ResizeEdge.Bottom,ResizeEdge.Left|ResizeEdge.Top,ResizeEdge.Right|ResizeEdge.Top,ResizeEdge.Left|ResizeEdge.Bottom,ResizeEdge.Right|ResizeEdge.Bottom}){
            var r=LayoutMath.Resize(box,new(9999,9999),edge,screen,16.0/9);
            Check($"Bordas respeitam monitor: {edge}",r.Width>0&&r.Height>0&&r.Left>=0&&r.Top>=0&&r.Right<=screen.Width&&r.Bottom<=screen.Height);
            Check($"Proporção preservada: {edge}",Math.Abs(r.Height-r.Width*9.0/16)<=1);
        }
        Check("Monitor com origem negativa",new Settings{X=1800,Y=1000,Width=420,Height=236}.Bounds(new(-1920,0,1920,1080))==new Rectangle(-420,844,420,236));
        Check("Fit centraliza 16:9",Settings.Fit(new(1920,1080),new(0,0,400,400))==new Rectangle(0,87,400,225));
        File.WriteAllLines(path,results);return failed==0?0:1;
    }
}
