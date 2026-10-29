using System.Diagnostics;
using System.Text.Json;
namespace PipOverlay;
internal static class WorkspaceTests
{
    internal static int Run(string report){
        var results=new List<string>();
        using(var form=new MainForm(true)){
            form.Show();Application.DoEvents();
            var all=new List<Control>();void Walk(Control c){all.Add(c);foreach(Control child in c.Controls)Walk(child);}Walk(form);
            var editor=all.OfType<LayoutEditor>().Single();int height=editor.Height;
            all.OfType<Button>().Single(x=>x.AccessibleName=="Mostrar ou ocultar ajustes avançados").PerformClick();Application.DoEvents();
            if(editor.Height!=height||editor.Width<650)throw new Exception("Settings displaced the editor");
            foreach(string section in new[]{"Imagem","Posição","Mais"}){all.OfType<Button>().Single(x=>x.Text==section).PerformClick();Application.DoEvents();if(editor.Height!=height)throw new Exception("Section reduced canvas height");}
            results.Add($"PASS sidebar preserves editor height {height}px across all sections, editor width {editor.Width}px");
            form.Size=form.MinimumSize;Application.DoEvents();
            foreach(string section in new[]{"Imagem","Posição","Mais"}){
                all.OfType<Button>().Single(x=>x.Text==section).PerformClick();Application.DoEvents();
                foreach(var flow in all.OfType<FlowLayoutPanel>().Where(x=>x.Visible&&x.AutoScroll))if(flow.HorizontalScroll.Visible)throw new Exception("Horizontal settings overflow at minimum window size");
            }
            var fps=all.OfType<ModernComboBox>().Single(x=>x.AccessibleName=="FPS · FLUIDEZ");
            all.OfType<Button>().Single(x=>x.Text=="Imagem").PerformClick();fps.Focus();fps.DroppedDown=true;Application.DoEvents();
            if(!fps.DroppedDown)throw new Exception("FPS selector did not open");fps.SelectedIndex=1;fps.DroppedDown=false;
            if(fps.Text!="30 fps")throw new Exception("FPS selection failed");
            var toggle=all.OfType<ModernToggle>().Single(x=>x.Text=="FPS automático");toggle.Checked=true;Application.DoEvents();
            if(!all.OfType<ModernComboBox>().Single(x=>x.AccessibleName=="MÍNIMO NO MODO AUTOMÁTICO").Enabled)throw new Exception("Adaptive toggle failed");
            results.Add("PASS minimum-size sections, dropdown selection and automatic-FPS toggle");form.Close();
        }
        int result=1;using var demo=new DemoSource{TestColor=Color.CadetBlue};
        demo.Shown+=async (_,_)=>{Process? host=null;try{
            demo.PauseAnimation();host=Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"PipTest32.exe"),"--timed"){UseShellExecute=false,CreateNoWindow=true});await Task.Delay(700);
            using var session=new GpuVideoSession(new WindowSource(demo.Handle,(uint)Environment.ProcessId,"Synthetic","Synthetic",true),new Settings{FramesPerSecond=30,Resolution="1080p"},new Size(1920,1080),new RenderTarget(host!.Id,"PipTest32",true));
            await Task.Delay(2000);long wakes=session.WorkerWakeups,frames=session.ProcessedFrames,rendered=session.Rendered;using var own=Process.GetCurrentProcess();var cpu=own.TotalProcessorTime;var clock=Stopwatch.StartNew();
            session.Apply(new Settings{FramesPerSecond=30,Resolution="1080p",X=1100,Y=600});await Task.Delay(5000);
            var metrics=new{seconds=clock.Elapsed.TotalSeconds,wakeups=session.WorkerWakeups-wakes,processed=session.ProcessedFrames-frames,rendered=session.Rendered-rendered,session.Published,session.Error,cpuCorePercent=(own.TotalProcessorTime-cpu).TotalSeconds/clock.Elapsed.TotalSeconds*100};
            result=session.Error==null&&session.Published>0&&session.Rendered>rendered&&metrics.wakeups<80?0:1;
            File.WriteAllText(report,JsonSerializer.Serialize(new{result,results,metrics},new JsonSerializerOptions{WriteIndented=true}));
        }catch(Exception ex){File.WriteAllText(report,JsonSerializer.Serialize(new{result,error=ex.Message,results}));}finally{if(host!=null){if(!host.HasExited)host.CloseMainWindow();host.Dispose();}demo.Close();}};
        Application.Run(demo);return result;
    }
}
