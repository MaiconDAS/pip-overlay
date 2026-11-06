using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text.Json;
namespace PipOverlay;

internal static class DesktopPerformanceTests
{
    sealed class GpuMeter:IDisposable{
        [DllImport("pdh.dll",CharSet=CharSet.Unicode)]static extern uint PdhOpenQuery(string? source,nint data,out nint query);
        [DllImport("pdh.dll",CharSet=CharSet.Unicode)]static extern uint PdhAddEnglishCounter(nint query,string path,nint data,out nint counter);
        [DllImport("pdh.dll")]static extern uint PdhCollectQueryData(nint query);
        [DllImport("pdh.dll")]static extern uint PdhGetFormattedCounterArrayW(nint counter,uint format,ref uint bytes,out uint count,nint buffer);
        [DllImport("pdh.dll")]static extern uint PdhCloseQuery(nint query);
        [StructLayout(LayoutKind.Sequential)]struct Value{public uint Status;public double Number;}
        [StructLayout(LayoutKind.Sequential)]struct Item{public nint Name;public Value Value;}
        nint query,counter;
        internal GpuMeter(){if(PdhOpenQuery(null,0,out query)!=0||PdhAddEnglishCounter(query,@"\GPU Engine(*)\Utilization Percentage",0,out counter)!=0){Dispose();return;}PdhCollectQueryData(query);}
        internal Dictionary<string,double>? Read(int pid){
            if(query==0||PdhCollectQueryData(query)!=0)return null;uint size=0,count;PdhGetFormattedCounterArrayW(counter,0x200,ref size,out count,0);if(size==0)return null;
            nint buffer=Marshal.AllocHGlobal((int)size);try{if(PdhGetFormattedCounterArrayW(counter,0x200,ref size,out count,buffer)!=0)return null;var result=new Dictionary<string,double>();
                for(int i=0;i<count;i++){var value=Marshal.PtrToStructure<Item>(buffer+i*Marshal.SizeOf<Item>());string name=Marshal.PtrToStringUni(value.Name)??"";if(!name.StartsWith("pid_"+pid+"_")||value.Value.Status>1)continue;int type=name.IndexOf("engtype_",StringComparison.Ordinal);string key=type<0?"unknown":name[(type+8)..];result[key]=result.GetValueOrDefault(key)+value.Value.Number;}return result;
            }finally{Marshal.FreeHGlobal(buffer);}
        }
        public void Dispose(){if(query!=0)PdhCloseQuery(query);query=counter=0;}
    }
    internal static int Source(){using var demo=new DemoSource{FormBorderStyle=FormBorderStyle.None,ClientSize=new(1920,1080),Location=Point.Empty,TopMost=false};using var map=MemoryMappedFile.CreateNew($"Local\\PipPerfSource-{Environment.ProcessId}",8);using var view=map.CreateViewAccessor();demo.Paint+=(_,_)=>view.Write(0,demo.PaintedFrames);Application.Run(demo);return 0;}
    internal static int Run(string report,int seconds=12,int rounds=3){
        int result=1;using var controller=new Form{Opacity=0,ShowInTaskbar=false,Width=1,Height=1};
        controller.Shown+=async (_,_)=>{
            Process? source=null,host=null;var samples=new List<object>();
            try{
                source=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--desktop-source"){UseShellExecute=false})!;
                host=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"PipTest32.exe"),"--long"){UseShellExecute=false})!;
                await Task.Delay(1500);source.Refresh();if(source.MainWindowHandle==0)throw new IOException("Synthetic source unavailable");
                using var sourceMap=MemoryMappedFile.OpenExisting($"Local\\PipPerfSource-{source.Id}");using var sourceView=sourceMap.CreateViewAccessor();
                using var self=Process.GetCurrentProcess();using var gpu=new GpuMeter();using var map=MemoryMappedFile.OpenExisting($"Local\\PipHost5-{host.Id}");using var view=map.CreateViewAccessor();
                for(int round=0;round<rounds;round++)foreach(int fps in round%2==0?new[]{0,30,60}:new[]{60,30,0}){
                    GpuVideoSession? session=null;DesktopOverlay? overlay=null;
                    try{
                        var settings=new Settings{Resolution="1080p",FramesPerSecond=fps==0?30:fps,Width=480,Height=270,X=500,Y=300};
                        if(fps!=0){overlay=new DesktopOverlay(settings,Screen.PrimaryScreen!.Bounds);overlay.ShowOverlay();session=new(new WindowSource(source.MainWindowHandle,(uint)source.Id,"Synthetic","Synthetic",true),settings,Screen.PrimaryScreen!.Bounds.Size,new(Environment.ProcessId,"Desktop",false,true,Backend:"desktop"));}
                        await Task.Delay(2000);self.Refresh();long memory=self.PrivateMemorySize64,sourceFrames=sourceView.ReadInt64(0);var cpu=self.TotalProcessorTime;var sourceCpu=source.TotalProcessorTime;var hostCpu=host.TotalProcessorTime;
                        long published=session?.Published??0,uploaded=session?.Uploaded??0,wakes=session?.WorkerWakeups??0;int first=view.ReadInt32(16);var clock=Stopwatch.StartNew();var costs=new List<double>();var engines=new List<Dictionary<string,double>>();gpu.Read(Environment.ProcessId);
                        for(int tick=0;tick<seconds*5;tick++){await Task.Delay(200);if(session?.Error is string error)throw new IOException(error);if(session!=null)costs.Add(session.PublishMilliseconds);if(tick%5==0&&gpu.Read(Environment.ProcessId) is {} reading)engines.Add(reading);}
                        double elapsed=clock.Elapsed.TotalSeconds;int end=view.ReadInt32(16);var frames=new List<float>();for(int index=Math.Max(first,end-4096);index<end;index++)frames.Add(view.ReadSingle(20+(index%4096)*4));frames.Sort();
                        double Percentile(double p)=>frames.Count==0?0:frames[Math.Min(frames.Count-1,(int)(frames.Count*p))];self.Refresh();
                        samples.Add(new{round,requestedFps=fps,sourceFps=(sourceView.ReadInt64(0)-sourceFrames)/elapsed,presentation=overlay?.GetType().GetMethod("Diagnostics",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(overlay,null),seconds=elapsed,appCpuOneCorePercent=(self.TotalProcessorTime-cpu).TotalSeconds/elapsed*100,sourceCpuOneCorePercent=(source.TotalProcessorTime-sourceCpu).TotalSeconds/elapsed*100,hostCpuOneCorePercent=(host.TotalProcessorTime-hostCpu).TotalSeconds/elapsed*100,privateBytes=self.PrivateMemorySize64,privateDelta=self.PrivateMemorySize64-memory,hostFps=(end-first)/elapsed,hostFrameMedianMs=Percentile(.5),hostFrameP95Ms=Percentile(.95),hostFrameP99Ms=Percentile(.99),publishedFps=((session?.Published??0)-published)/elapsed,uploadedFps=((session?.Uploaded??0)-uploaded)/elapsed,workerWakeups=((session?.WorkerWakeups??0)-wakes),submitMeanMs=costs.Count==0?0:costs.Average(),gpuEngineSamples=engines,transferWidth=session?.TransferSize.Width,transferHeight=session?.TransferSize.Height,transport=session?.SharedTransport});
                        if(session!=null&&(session.Uploaded<=uploaded||session.TransferSize!=new Size(1920,1080)))throw new IOException("Expected 1080p delivery missing");
                    }finally{session?.Dispose();overlay?.Dispose();}
                }
                result=0;
            }catch(Exception ex){samples.Add(new{error=ex.ToString()});}
            finally{
                foreach(var process in new[]{source,host})if(process!=null){if(!process.HasExited){process.CloseMainWindow();if(!process.WaitForExit(3000))process.Kill();}process.Dispose();}
                File.WriteAllText(report,JsonSerializer.Serialize(new{result,version=UpdateService.Current.ToString(),note="Synthetic source and D3D9 host; CPU is percent of one logical core; GPU values sum per-process engine instances, not total GPU utilization.",samples},new JsonSerializerOptions{WriteIndented=true}));controller.Close();
            }
        };Application.Run(controller);return result;
    }
}
