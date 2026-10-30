using System.Diagnostics;
using System.Text.Json;
using System.IO.MemoryMappedFiles;
namespace PipOverlay;
internal static class GpuValidation
{
    internal static int Benchmark(string report,int fps=30,string resolution="1080p",bool fullSource=false)
    {
        int result=1;using var demo=new DemoSource();if(fullSource){demo.FormBorderStyle=FormBorderStyle.None;demo.ClientSize=new Size(1920,1080);demo.Location=Point.Empty;demo.TopMost=false;}
        demo.Shown+=async (_,_)=>{
            Process? host=null;GpuVideoSession? session=null;var phases=new List<object>();
            try {
                ShowWindow(demo.Handle,4);await Task.Delay(150);
                host=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"PipTest32.exe"),"--timed"){UseShellExecute=false,CreateNoWindow=true})!;
                await Task.Delay(700);using var map=MemoryMappedFile.OpenExisting($"Local\\PipHost5-{host.Id}",MemoryMappedFileRights.Read);using var view=map.CreateViewAccessor(0,0,MemoryMappedFileAccess.Read);
                var source=new WindowSource(demo.Handle,(uint)Environment.ProcessId,"Teste","Demonstração",true);
                using var process=Process.GetCurrentProcess();
                foreach(string mode in new[]{"baseline","direct3d","baseline-final"}) {
                    if(mode=="direct3d")session=new GpuVideoSession(source,new Settings{FramesPerSecond=fps,Resolution=resolution},new Size(1920,1080),new RenderTarget(host.Id,"PipTest32",true));
                    await Task.Delay(500);var clock=Stopwatch.StartNew();var ownCpu=process.TotalProcessorTime;var hostCpu=host.TotalProcessorTime;long sourceFrames=demo.PaintedFrames;int f0=view.ReadInt32(0),sample0=view.ReadInt32(16);long sent=session?.Published??0,shown=session?.Uploaded??0;var costs=new List<double>();
                    for(int i=0;i<30;i++){await Task.Delay(200);if(session?.Error is string error)throw new InvalidOperationException(error);if(session!=null)costs.Add(session.PublishMilliseconds);}
                    double seconds=clock.Elapsed.TotalSeconds;int count=view.ReadInt32(0)-f0;var frameTimes=new List<float>();int sampleEnd=view.ReadInt32(16);for(int n=Math.Max(sample0,sampleEnd-4096);n<sampleEnd;n++)frameTimes.Add(view.ReadSingle(20+(n%4096)*4));frameTimes.Sort();
                    phases.Add(new{mode,seconds,sourceFps=(demo.PaintedFrames-sourceFrames)/seconds,hostFps=count/seconds,p95FrameMs=frameTimes.Count>0?frameTimes[(int)((frameTimes.Count-1)*.95)]:0,producerCpuCorePercent=(process.TotalProcessorTime-ownCpu).TotalSeconds/seconds*100,hostCpuCorePercent=(host.TotalProcessorTime-hostCpu).TotalSeconds/seconds*100,sentFps=((session?.Published??0)-sent)/seconds,uploadedFps=((session?.Uploaded??0)-shown)/seconds,meanSubmitMs=costs.Count==0?0:costs.Average(),transferWidth=session?.TransferSize.Width,transferHeight=session?.TransferSize.Height,processed=session?.ProcessedFrames,discarded=session?.DiscardedFrames,stateErrors=view.ReadInt32(8)});
                    session?.Dispose();session=null;
                }
                result=view.ReadInt32(8)==0?0:1;
                File.WriteAllText(report,JsonSerializer.Serialize(new{result,source="Synthetic animated WinForms source",requestedFps=fps,resolution,sourceWidth=demo.ClientSize.Width,sourceHeight=demo.ClientSize.Height,fullscreen=false,pixelReadbackValidation=false,phases},new JsonSerializerOptions{WriteIndented=true}));
            }catch(Exception e){File.WriteAllText(report,JsonSerializer.Serialize(new{error=e.ToString(),phases},new JsonSerializerOptions{WriteIndented=true}));}
            finally{session?.Dispose();if(host!=null){if(!host.HasExited){host.CloseMainWindow();if(!host.WaitForExit(3000)){host.Kill();host.WaitForExit();}}host.Dispose();}demo.Close();}
        };
        Application.Run(demo);return result;
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool ShowWindow(nint window,int command);
    internal static int Run(string report,bool fullscreen,bool browser)
    {
        int result=1;using var demo=new DemoSource();
        demo.ClientSize=new Size(1280,720);
        demo.Shown+=async (_,_)=>{
            Process? host=null;GpuVideoSession? session=null;SourceLease? sourceLease=null;
            var samples=new List<object>();
            try {
                ShowWindow(demo.Handle,4);await Task.Delay(150);
                host=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"PipTest32.exe"),(fullscreen?"--fullscreen ":"")+"--timed"){UseShellExecute=false,CreateNoWindow=true})!;
                await Task.Delay(700);
                if(host.HasExited)throw new InvalidOperationException($"Host encerrou: {host.ExitCode}");
                var source=browser?WindowSource.Find().First(x=>x.LikelyPip):new WindowSource(demo.Handle,(uint)Environment.ProcessId,"Teste","Demonstração",true);
                sourceLease=new SourceLease(source);
                var settings=new Settings{X=24,Y=24,Width=480,Height=270,FramesPerSecond=30};
                session=new GpuVideoSession(source,settings,new Size(1920,1080),new RenderTarget(host.Id,"PipTest32",true));
                if(fullscreen)await Task.Delay(3500);
                using var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;long firstPublished=session.Published;var clock=Stopwatch.StartNew();
                foreach(var box in new[]{new Rectangle(24,24,480,270),new Rectangle(1416,24,480,270),new Rectangle(1416,786,480,270),new Rectangle(24,786,480,270),new Rectangle(480,270,960,540)}) {
                    session.Apply(settings with{X=box.X,Y=box.Y,Width=box.Width,Height=box.Height});session.ValidatePixels();await Task.Delay(2500);
                    var pixels=session.ValidatePixels();samples.Add(new{box.X,box.Y,box.Width,box.Height,session.TransferSize,session.Published,session.Rendered,session.Uploaded,session.Ready,session.ActualFullscreen,session.TargetSize,session.Error,session.PublishMilliseconds,verified=pixels[0],mismatched=pixels[1]});
                    if(session.Error!=null)throw new InvalidOperationException(session.Error);
                }
                var seconds=clock.Elapsed.TotalSeconds;
                var check=session.ValidatePixels();result=session.Rendered>0&&session.Uploaded>10&&check[0]>=5&&check[1]==0&&session.TransferSize.Width>640&&(!fullscreen||session.ActualFullscreen)?0:1;
                File.WriteAllText(report,JsonSerializer.Serialize(new{result,fullscreen,browser,seconds,hostPid=host.Id,source=source.Browser,outputFps=(session.Published-firstPublished)/seconds,cpuCorePercent=(process.TotalProcessorTime-cpu).TotalSeconds/seconds*100,samples},new JsonSerializerOptions{WriteIndented=true}));
            }catch(Exception e){File.WriteAllText(report,JsonSerializer.Serialize(new{error=e.ToString(),samples},new JsonSerializerOptions{WriteIndented=true}));}
            finally{session?.Dispose();sourceLease?.Dispose();if(host!=null){if(!host.HasExited){host.CloseMainWindow();if(!host.WaitForExit(3000)){host.Kill();host.WaitForExit();}}host.Dispose();}demo.Close();}
        };
        Application.Run(demo);return result;
    }
}
