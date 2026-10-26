using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace PipOverlay;
internal static class ValidationV4
{
    internal static int Run(string report)
    {
        var rows=new List<string>();int failed=0;
        void Check(string name,bool ok){rows.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failed++;}
        var screen=new Size(1920,1080);var original=new Rectangle(500,300,480,270);
        Check("25% da largura corresponde a 480 pixels em 1920",original.Width==screen.Width/4);
        foreach(var delta in new[]{new Point(-9999,-9999),new Point(9999,9999),new Point(300,200)}) {
            var r=LayoutMath.Drag(original,delta,screen);Check("arraste mantém tamanho e limites "+delta,r.Size==original.Size&&new Rectangle(Point.Empty,screen).Contains(r));
        }
        foreach(var edge in new[]{ResizeEdge.Left,ResizeEdge.Right,ResizeEdge.Top,ResizeEdge.Bottom,ResizeEdge.Left|ResizeEdge.Top,ResizeEdge.Right|ResizeEdge.Top,ResizeEdge.Left|ResizeEdge.Bottom,ResizeEdge.Right|ResizeEdge.Bottom})
        foreach(var d in new[]{new Point(-5000,-5000),new Point(5000,5000),new Point(80,40)}) {
            var r=LayoutMath.Resize(original,d,edge,screen,16.0/9);
            Check($"redimensionar {edge} {d}: proporção e limites",new Rectangle(Point.Empty,screen).Contains(r)&&Math.Abs(r.Height-r.Width*9.0/16)<=1);
        }
        using(var editor=new LayoutEditor {Size=new Size(1000,650),MonitorSize=screen}) {
            var full=editor.ToPreview(new Rectangle(Point.Empty,screen));var quarter=editor.ToPreview(new Rectangle(1440,810,480,270));
            Check("editor representa canto inferior direito com escala proporcional",Math.Abs(quarter.Width*4-full.Width)<=2&&Math.Abs(quarter.Right-full.Right)<=1&&Math.Abs(quarter.Bottom-full.Bottom)<=1);
        }
        string tag=RtssFrameOutput.Hypertext("C:\\test.bmp",new Rectangle(1440,810,480,270),new Size(480,270));
        Check("RTSS inclui posição, camada, limites e escala 1",tag.Contains("<P=1440,810><L0><E=480,270>")&&tag.Contains("<FNT=Segoe UI,-16,400,1>"));
        rows.Add($"Resultado: {rows.Count-failed}/{rows.Count} aprovados. Renderização RTSS precisa de validação visual separada.");File.WriteAllLines(report,rows);return failed==0?0:1;
    }
    internal static int PositionProbe(string report)
    {
        try {
            var screen=Screen.PrimaryScreen!.Bounds.Size;using var output=new RtssFrameOutput();using var bitmap=new Bitmap(480,270);
            var points=new[]{new Point(24,24),new Point(screen.Width-504,24),new Point(screen.Width-504,screen.Height-294),new Point(24,screen.Height-294),new Point((screen.Width-480)/2,(screen.Height-270)/2)};
            for(int i=0;i<points.Length;i++) {
                using(var g=Graphics.FromImage(bitmap)){g.Clear(i%2==0?Color.DarkBlue:Color.DarkRed);g.DrawString($"Posição {i+1}\n480 × 270",SystemFonts.DefaultFont,Brushes.White,20,30);}
                var clock=Stopwatch.StartNew();while(clock.Elapsed.TotalSeconds<6){output.Publish(bitmap,new Settings{X=points[i].X,Y=points[i].Y,Width=480,Height=270});Thread.Sleep(100);}
            }
            File.WriteAllText(report,"Cinco posições publicadas; validar visualmente 480x270 e coordenadas.\n");return 0;
        }catch(Exception e){File.WriteAllText(report,e.ToString());return 1;}
    }
    internal static int Benchmark(string report)
    {
        var rows=new List<object>();
        try {
            var source=WindowSource.Find().Single(x=>x.LikelyPip);
            using var metrics=new GameMetrics();
            foreach(string mode in new[]{"baseline","v03","v04","baseline-final"}) {
                WindowCapture? capture=null;LegacyRtssFrameOutput? old=null;RtssFrameOutput? current=null;
                try {
                    if(mode=="v03"){old=new();capture=new WindowCapture(source.Handle);}
                    if(mode=="v04"){current=new();capture=new WindowCapture(source.Handle);}
                    var process=Process.GetCurrentProcess();TimeSpan cpu=process.TotalProcessorTime;var clock=Stopwatch.StartNew();long sent=0;var costs=new List<double>();var fps=new List<double>();long nextSample=0;
                    while(clock.Elapsed.TotalSeconds<12) {
                        long begin=Environment.TickCount64;
                        if(clock.ElapsedMilliseconds>=nextSample){var sample=metrics.Sample();if(sample is double f)fps.Add(f);nextSample=clock.ElapsedMilliseconds+500;}
                        using var frame=capture?.TakeFrame();
                        if(frame!=null){var cost=Stopwatch.StartNew();bool ok=old?.Publish(frame,new Settings())??current!.Publish(frame,new Settings());if(ok)sent++;costs.Add(cost.Elapsed.TotalMilliseconds);}
                        int period=mode=="v03"?67:33;
                        // v0.3 added all preparation cost to its nominal interval.
                        Thread.Sleep(mode=="v03"?period:(int)Math.Max(1,period-(Environment.TickCount64-begin)));
                    }
                    double seconds=clock.Elapsed.TotalSeconds;
                    rows.Add(new{mode,seconds,sent,outputFps=sent/seconds,cpuCorePercent=(process.TotalProcessorTime-cpu).TotalSeconds/seconds*100,prepareMs=costs.Count>0?costs.Average():0,gameFpsSamples=fps,gameFpsMean=fps.Count>0?fps.Average():(double?)null});
                    File.WriteAllText(report,JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
                }finally{capture?.Dispose();old?.Dispose();current?.Dispose();}
            }
            return 0;
        }catch(Exception e){File.WriteAllText(report,JsonSerializer.Serialize(new{completed=rows,error=e.ToString()},new JsonSerializerOptions{WriteIndented=true}));return 1;}
    }
    sealed class GameMetrics : IDisposable
    {
        readonly MemoryMappedFile map=MemoryMappedFile.OpenExisting("RTSSSharedMemoryV2",MemoryMappedFileRights.Read);
        readonly MemoryMappedViewAccessor view;
        uint previous;
        internal GameMetrics(){view=map.CreateViewAccessor(0,0,MemoryMappedFileAccess.Read);}
        internal double? Sample(){
            if(view.Capacity<40||view.ReadUInt32(0)!=RtssClient.Signature)return null;
            long start=view.ReadUInt32(12),stride=view.ReadUInt32(8),count=view.ReadUInt32(16);
            if(start<40||stride<284||stride>1048576||count>1024||start+stride*count>view.Capacity)return null;
            var bytes=new byte[260];for(int i=0;i<count;i++){
                long at=start+i*stride;view.ReadArray(at+4,bytes,0,260);string name=Encoding.ASCII.GetString(bytes).TrimEnd('\0');
                if(!name.Contains("DigimonMasters.exe",StringComparison.OrdinalIgnoreCase))continue;
                uint t0=view.ReadUInt32(at+268),t1=view.ReadUInt32(at+272),frames=view.ReadUInt32(at+276);
                uint span=unchecked(t1-t0);if(t1==previous||span==0||span>10000)return null;previous=t1;return frames*1000.0/span;
            }return null;
        }
        public void Dispose(){view.Dispose();map.Dispose();}
    }
}
