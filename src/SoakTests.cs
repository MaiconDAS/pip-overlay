using System.Diagnostics;
using System.Text.Json;
namespace PipOverlay;
internal static class SoakTests
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool PostMessage(nint window,uint message,nint w,nint l);
    internal static int Run(string report){
        int result=1;using var control=new Form{ShowInTaskbar=false,Opacity=0,Width=1,Height=1};
        control.Shown+=async (_,_)=>{Process? host=null;var rounds=new List<object>();var clock=Stopwatch.StartNew();long first=0,last=0;try{
            host=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"PipTest32.exe"),"--long"){UseShellExecute=false})!;await Task.Delay(500);
            for(int cycle=0;cycle<30;cycle++){
                using var sourceForm=new DemoSource();sourceForm.ClientSize=cycle%2==0?new Size(640,360):new Size(1280,720);sourceForm.Show();await Task.Delay(150);
                var source=new WindowSource(sourceForm.Handle,(uint)Environment.ProcessId,"Synthetic","Synthetic",true);
                long sent=0;using(var lease=new SourceLease(source)){
                    using var session=new GpuVideoSession(source,new Settings{Resolution=cycle%2==0?"720p":"1080p",FramesPerSecond=30},new Size(1920,1080),new RenderTarget(host.Id,"PipTest32",true));
                    await Task.Delay(2400);session.Apply(new Settings{Resolution="480p",FramesPerSecond=15,X=1200,Y=600,Width=640,Height=360});
                    host.Refresh();PostMessage(host.MainWindowHandle,0x201,1,(nint)((200<<16)|200));PostMessage(host.MainWindowHandle,0x202,0,(nint)((200<<16)|200));
                    await Task.Delay(2600);if(session.Error!=null||session.Published<10||session.Rendered<10)throw new IOException("Capture/reconnect failed at cycle "+cycle);sent=session.Published;
                }
                sourceForm.Close();await Task.Delay(400);GC.Collect();GC.WaitForPendingFinalizers();
                using var own=Process.GetCurrentProcess();last=own.PrivateMemorySize64;if(cycle==2)first=last;rounds.Add(new{cycle,published=sent,privateBytes=last});
                if(host.HasExited)throw new IOException("Host exited: "+host.ExitCode+" after "+clock.Elapsed.TotalSeconds+"s");
            }
            using var mapping=System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting($"Local\\PipHost5-{host.Id}");using var view=mapping.CreateViewAccessor();
            int clicks=view.ReadInt32(4),stateErrors=view.ReadInt32(8);result=stateErrors==0&&clicks==30&&last-first<128L*1024*1024?0:1;
            File.WriteAllText(report,JsonSerializer.Serialize(new{result,seconds=clock.Elapsed.TotalSeconds,cycles=rounds.Count,clicks,stateErrors,privateGrowthBytes=last-first,rounds},new JsonSerializerOptions{WriteIndented=true}));
        }catch(Exception ex){File.WriteAllText(report,JsonSerializer.Serialize(new{result,seconds=clock.Elapsed.TotalSeconds,error=ex.Message,rounds},new JsonSerializerOptions{WriteIndented=true}));}
        finally{if(host!=null){if(!host.HasExited)host.CloseMainWindow();host.Dispose();}control.Close();}};
        Application.Run(control);return result;
    }
}
