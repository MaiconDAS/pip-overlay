using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text.Json;
namespace PipOverlay;
internal static class ContentionTest
{
    internal static unsafe bool Lock(MemoryMappedViewAccessor view){byte* p=null;view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);try{return Interlocked.CompareExchange(ref *(int*)(p+view.PointerOffset+8),1,0)==0;}finally{view.SafeMemoryMappedViewHandle.ReleasePointer();}}
    internal static int Run(string report){
        int result=1;using var form=new Form{ShowInTaskbar=false,Opacity=0,Width=1,Height=1};using var timer=new System.Windows.Forms.Timer{Interval=50};
        form.Shown+=async (_,_)=>{Process? host=null;try{
            host=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"PipTest32.exe"),"--timed"){UseShellExecute=false})!;
            using var mapping=MemoryMappedFile.CreateOrOpen($"Local\\PipOverlay8-{host.Id}",72+1920*1080*4+24);using var view=mapping.CreateViewAccessor();int tail=72+1920*1080*4;
            view.Write(0,0x38504950);view.Write(4,1);view.Write(16,640);view.Write(20,360);view.Write(32,.1f);view.Write(36,.1f);view.Write(40,.4f);view.Write(44,.4f);
            byte[] pixels=new byte[640*360*4];for(int i=0;i<pixels.Length;i+=4){pixels[i]=30;pixels[i+1]=210;pixels[i+2]=80;pixels[i+3]=255;}view.WriteArray(72,pixels,0,pixels.Length);view.Write(24,1);view.Write(12,1);view.Write(tail,1);
            timer.Tick+=(_,_)=>view.Write(28,unchecked((uint)Environment.TickCount));timer.Start();view.Write(28,unchecked((uint)Environment.TickCount));
            PortableSetup.Ensure(32);using(var attach=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"PipAttach32.exe"),host.Id.ToString()){UseShellExecute=false,CreateNoWindow=true})!){await attach.WaitForExitAsync();if(attach.ExitCode!=0)throw new IOException("Attach failed");}
            await Task.Delay(2000);var deadline=Stopwatch.StartNew();while(!Lock(view)){if(deadline.ElapsedMilliseconds>1000)throw new IOException("Lock timeout");await Task.Delay(10);}
            int before=view.ReadInt32(48),verified=view.ReadInt32(tail+4);int after,checks;
            try{await Task.Delay(1800);after=view.ReadInt32(48);checks=view.ReadInt32(tail+4)-verified;}finally{view.Write(8,0);}
            int mismatched=view.ReadInt32(tail+8);result=after-before>20&&checks>=2&&mismatched==0?0:1;
            File.WriteAllText(report,JsonSerializer.Serialize(new{result,framesWhileLocked=after-before,pixelChecksWhileLocked=checks,mismatched},new JsonSerializerOptions{WriteIndented=true}));view.Write(12,0);
        }catch(Exception e){File.WriteAllText(report,JsonSerializer.Serialize(new{error=e.Message}));}finally{timer.Stop();if(host!=null){if(!host.HasExited)host.CloseMainWindow();host.Dispose();}form.Close();}};
        Application.Run(form);return result;
    }
}
