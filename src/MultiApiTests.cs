using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text.Json;
namespace PipOverlay;
internal static class MultiApiTests
{
    internal static int Run(string executable,string report,string arguments){
        int result=1;using var timer=new System.Windows.Forms.Timer{Interval=16};
        using var form=new Form{ShowInTaskbar=false,Opacity=0,Width=1,Height=1};
        form.Shown+=async (_,_)=>{
            Process? process=null;var samples=new List<object>();
            try{
                var target=await GameLauncher.Launch(executable,arguments);process=Process.GetProcessById(target.Pid);
                using var mapping=MemoryMappedFile.CreateOrOpen($"Local\\PipOverlay8-{target.Pid}",72+1920*1080*4+24);
                using var view=mapping.CreateViewAccessor();int tail=72+1920*1080*4;
                view.Write(0,0x38504950);view.Write(4,1);view.Write(16,640);view.Write(20,360);
                byte[] frame=new byte[640*360*4];for(int i=0;i<frame.Length;i+=4){frame[i]=30;frame[i+1]=210;frame[i+2]=80;frame[i+3]=255;}
                view.WriteArray(72,frame,0,frame.Length);view.Write(24,1);view.Write(12,1);
                timer.Tick+=(_,_)=>view.Write(28,unchecked((uint)Environment.TickCount));timer.Start();
                foreach(var box in new[]{new Rectangle(24,24,480,270),new Rectangle(1416,24,480,270),new Rectangle(1416,786,480,270),new Rectangle(24,786,480,270),new Rectangle(480,270,960,540)}){
                    view.Write(tail,0);view.Write(8,1);view.Write(32,box.X/1920f);view.Write(36,box.Y/1080f);view.Write(40,box.Width/1920f);view.Write(44,box.Height/1080f);view.Write(8,0);
                    await Task.Delay(2200);view.Write(tail,1);await Task.Delay(1700);
                    samples.Add(new{box,ready=view.ReadInt32(60),rendered=view.ReadInt32(48),uploaded=view.ReadInt32(52),verified=view.ReadInt32(tail+4),mismatched=view.ReadInt32(tail+8),width=view.ReadInt32(tail+16),height=view.ReadInt32(tail+20)});
                    if(process.HasExited)throw new IOException("O processo de teste encerrou antes da validação. Consulte ReShade.log e vulkan-loader.txt.");
                }
                result=view.ReadInt32(tail+4)>=5&&view.ReadInt32(tail+8)==0&&view.ReadInt32(48)>0?0:1;
                view.Write(12,0);timer.Stop();
                File.WriteAllText(report,JsonSerializer.Serialize(new{result,samples},new JsonSerializerOptions{WriteIndented=true}));
            }catch(Exception ex){File.WriteAllText(report,JsonSerializer.Serialize(new{error=ex.ToString(),samples}));}
            finally{timer.Stop();if(process!=null){if(!process.HasExited)process.CloseMainWindow();process.Dispose();}form.Close();}
        };
        Application.Run(form);return result;
    }
}
