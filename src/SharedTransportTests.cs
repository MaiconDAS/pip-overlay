using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text.Json;
namespace PipOverlay;
internal static class SharedTransportTests
{
    internal static int Run(string report,string mode,int bits){
        int result=1;bool pixels=mode=="pixels";GpuVideoSession.BenchmarkCpuTransport=mode=="cpu";
        using var demo=new DemoSource{FormBorderStyle=FormBorderStyle.None,ClientSize=new Size(1920,1080),Location=Point.Empty,TopMost=false,TestColor=pixels?Color.CadetBlue:null};
        demo.Shown+=async (_,_)=>{Process? host=null;GpuVideoSession? session=null;var observations=new List<object>();try{
            RenderTarget target;if(bits==9){string generic=Path.Combine(AppContext.BaseDirectory,"GenericGame32.exe");File.Copy(Path.Combine(AppContext.BaseDirectory,"PipTest32.exe"),generic,true);host=Process.Start(new ProcessStartInfo(generic,"--long"){UseShellExecute=false})!;target=new(host.Id,"PipTest32",true);}else{target=await GameLauncher.Launch(Path.Combine(AppContext.BaseDirectory,$"PipModern{bits}.exe"),"d3d11");host=Process.GetProcessById(target.Pid);}await Task.Delay(2300);
            var detected=GameDiscovery.Find(force:true).FirstOrDefault(x=>x.Pid==target.Pid);
            if(detected==null)throw new Exception("Generic window discovery missed the test host");
            if(bits!=9&&detected.UnavailableReason==null)throw new Exception("Unprepared modern renderer incorrectly marked ready");
            observations.Add(new{discovery="generic-window",detected.Name,detected.Is32Bit,requiresPreparation=detected.UnavailableReason!=null});
            using var self=Process.GetCurrentProcess();var cpu0=self.TotalProcessorTime;var host0=host.TotalProcessorTime;var pre=Stopwatch.StartNew();await Task.Delay(2500);double baseCpu=(self.TotalProcessorTime-cpu0).TotalSeconds/pre.Elapsed.TotalSeconds*100,baseHost=(host.TotalProcessorTime-host0).TotalSeconds/pre.Elapsed.TotalSeconds*100;
            var settings=new Settings{Resolution="1080p",FramesPerSecond=60,X=24,Y=24,Width=480,Height=270};
            session=new GpuVideoSession(new WindowSource(demo.Handle,(uint)Environment.ProcessId,"Synthetic","Synthetic",true),settings,new Size(1920,1080),target);
            await Task.Delay(1800);
            using var map=MemoryMappedFile.OpenExisting($"Local\\PipOverlay8-{target.Pid}");using var view=map.CreateViewAccessor();
            uint[] CheckPixels(){var values=session.ValidatePixels();view.Write(72+1920*1080*4,2);return values;}
            if(mode=="reconnect"){
                long before=self.PrivateMemorySize64;
                for(int cycle=0;cycle<8;cycle++){
                    session.Dispose();session=new GpuVideoSession(new WindowSource(demo.Handle,(uint)Environment.ProcessId,"Synthetic","Synthetic",true),settings with{Resolution=cycle%2==0?"720p":"1080p"},new Size(1920,1080),target);
                    await Task.Delay(1500);if(session.SharedTransport!=1||session.Uploaded<5||session.Error!=null)throw new Exception("GPU reconnect failed at "+cycle);
                }
                GC.Collect();GC.WaitForPendingFinalizers();self.Refresh();long growth=self.PrivateMemorySize64-before;
                observations.Add(new{cycles=8,privateGrowthBytes=growth,session.SharedTransport});if(growth>96L*1024*1024)throw new Exception("Unexpected reconnect memory growth");
            }else if(pixels){
                var color=Color.CadetBlue;var expected=new byte[1920*1080*4];for(int i=0;i<expected.Length;i+=4){expected[i]=color.B;expected[i+1]=color.G;expected[i+2]=color.R;expected[i+3]=255;}
                while(!ContentionTest.Lock(view))await Task.Delay(5);try{view.WriteArray(72,expected,0,expected.Length);}finally{view.Write(8,0);}
                CheckPixels();
                foreach(var rect in new[]{new Rectangle(24,24,480,270),new Rectangle(1416,786,480,270),new Rectangle(480,270,960,540)}){
                    view.Write(72+1920*1080*4,0);session.Apply(settings with{X=rect.X,Y=rect.Y,Width=rect.Width,Height=rect.Height});await Task.Delay(400);CheckPixels();await Task.Delay(1200);var check=CheckPixels();observations.Add(new{rect,session.SharedTransport,session.Published,session.Uploaded,verified=check[0],mismatched=check[1],session.Error});
                    if(session.SharedTransport!=(bits==9?0:1)||check[1]!=0)throw new Exception("Shared GPU pixel or activation failure");
                }
                view.Write(72+1920*1080*4,0);session.Apply(settings with{Resolution="720p"});await Task.Delay(400);CheckPixels();await Task.Delay(1200);if(session.SharedTransport!=(bits==9?0:1)||session.TransferSize.Width!=1280)throw new Exception("Shared resize failed");
                if(bits!=9){using var gpuMap=MemoryMappedFile.OpenExisting($"Local\\PipGpu2-{target.Pid}");using var gpuView=gpuMap.CreateViewAccessor();demo.PauseAnimation();await Task.Delay(600);gpuView.Write(16,1);await Task.Delay(1600);
                var end=CheckPixels();if(session.SharedTransport!=-1||end[0]<3||end[1]!=0)throw new Exception("Fallback or pixel check failed");
                observations.Add(new{fallback=session.SharedTransport,verified=end[0],mismatched=end[1]});}
            }else{
                cpu0=self.TotalProcessorTime;host0=host.TotalProcessorTime;long sent=session.Published,uploaded=session.Uploaded,frames=demo.PaintedFrames;var clock=Stopwatch.StartNew();var costs=new List<double>();
                for(int i=0;i<40;i++){await Task.Delay(200);costs.Add(session.PublishMilliseconds);if(session.Error!=null)throw new Exception(session.Error);}
                double seconds=clock.Elapsed.TotalSeconds;observations.Add(new{seconds,sourceFps=(demo.PaintedFrames-frames)/seconds,sentFps=(session.Published-sent)/seconds,uploadedFps=(session.Uploaded-uploaded)/seconds,baseCpu,baseHost,producerCpu=(self.TotalProcessorTime-cpu0).TotalSeconds/seconds*100,hostCpu=(host.TotalProcessorTime-host0).TotalSeconds/seconds*100,submitMs=costs.Average(),session.SharedTransport,session.TransferSize});
                if(session.Published<=sent||session.Uploaded<=uploaded||session.SharedTransport!=(mode=="cpu"?0:1))throw new Exception("Expected transport did not deliver frames");
            }
            result=0;
        }catch(Exception ex){observations.Add(new{error=ex.ToString()});}finally{session?.Dispose();if(host!=null){if(!host.HasExited){host.CloseMainWindow();if(!host.WaitForExit(3000)){host.Kill();host.WaitForExit();}}host.Dispose();}File.WriteAllText(report,JsonSerializer.Serialize(new{result,mode,bits,observations},new JsonSerializerOptions{WriteIndented=true}));demo.Close();}};
        Application.Run(demo);return result;
    }
}
