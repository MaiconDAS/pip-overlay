using System.Runtime.InteropServices;
using System.Text.Json;
namespace PipOverlay;
internal static class DesktopOverlayTests
{
    [DllImport("user32.dll")] static extern nint WindowFromPoint(Point p);
    [DllImport("user32.dll")] static extern nint GetForegroundWindow();
    delegate void WinEvent(nint hook,uint kind,nint window,int objectId,int childId,uint thread,uint time);
    [DllImport("user32.dll")] static extern nint SetWinEventHook(uint first,uint last,nint module,WinEvent callback,uint process,uint thread,uint flags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(nint hook);
    internal static int Run(string report){
        int result=1;var observations=new List<object>();
        using var demo=new DemoSource{TestColor=Color.CadetBlue,ClientSize=new Size(640,360),Location=Point.Empty};
        demo.Shown+=async (_,_)=>{
            GpuVideoSession? session=null;DesktopOverlay? overlay=null;var foregroundWindows=new List<nint>();
            WinEvent callback=(_,_,window,_,_,_,_)=>foregroundWindows.Add(window);
            nint foregroundHook=SetWinEventHook(3,3,0,callback,0,0,0);
            using var underneath=new Form{Text="PiP isolated hit-test target",TopMost=true,FormBorderStyle=FormBorderStyle.None,StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(500,250,600,400),BackColor=Color.Magenta};
            try{
                underneath.Show();await Task.Delay(300);nint focus=GetForegroundWindow();
                var settings=new Settings{X=550,Y=300,Width=400,Height=225,Resolution="720p",FramesPerSecond=30};
                int activations=0;overlay=new DesktopOverlay(settings,Screen.PrimaryScreen!.Bounds);overlay.Activated+=(_,_)=>activations++;overlay.ShowOverlay();bool showPreservedFocus=GetForegroundWindow()==focus;
                session=new GpuVideoSession(new WindowSource(demo.Handle,(uint)Environment.ProcessId,"Synthetic","Synthetic",true),settings,Screen.PrimaryScreen.Bounds.Size,new(Environment.ProcessId,"Desktop",false,true,Backend:"desktop"));
                await Task.Delay(2300);
                var center=new Point(750,410);using var shot=new Bitmap(1,1);using(var graphics=Graphics.FromImage(shot))graphics.CopyFromScreen(center,Point.Empty,new Size(1,1));var pixel=shot.GetPixel(0,0);
                bool color=Math.Abs(pixel.R-Color.CadetBlue.R)<20&&Math.Abs(pixel.G-Color.CadetBlue.G)<20&&Math.Abs(pixel.B-Color.CadetBlue.B)<20;
                nint hit=WindowFromPoint(center);observations.Add(new{hit=hit.ToString(),under=underneath.Handle.ToString(),over=overlay.Handle.ToString(),underBounds=underneath.Bounds,overBounds=overlay.Bounds});
                bool clickThrough=hit==underneath.Handle,focusPreserved=activations==0&&foregroundHook!=0&&showPreservedFocus&&GetForegroundWindow()!=overlay.Handle&&!foregroundWindows.Contains(overlay.Handle);
                observations.Add(new{session.SharedTransport,session.Published,session.Uploaded,session.Error,pixel=pixel.ToString(),color,clickThrough,focusPreserved,showPreservedFocus,activations,focusBefore=focus.ToString(),focusAfter=GetForegroundWindow().ToString()});
                if(!color||!clickThrough||!focusPreserved||session.SharedTransport!=1||session.Uploaded<5)throw new Exception("Independent overlay validation failed");
                underneath.TopMost=false;underneath.TopMost=true;await Task.Delay(500);
                using(var graphics=Graphics.FromImage(shot))graphics.CopyFromScreen(center,Point.Empty,new Size(1,1));pixel=shot.GetPixel(0,0);
                bool restacked=Math.Abs(pixel.R-Color.CadetBlue.R)<20&&Math.Abs(pixel.G-Color.CadetBlue.G)<20&&Math.Abs(pixel.B-Color.CadetBlue.B)<20;
                observations.Add(new{restacked,activations});if(!restacked||activations!=0||foregroundWindows.Contains(overlay.Handle))throw new Exception("Target activation covered or focused the overlay");
                overlay.Apply(settings with{Width=320,Height=180,X=580,Y=330},Screen.PrimaryScreen.Bounds);await Task.Delay(1000);
                if(session.Error!=null||session.Uploaded<10)throw new Exception("Resize stopped capture");result=0;
            }catch(Exception ex){observations.Add(new{error=ex.ToString()});}
            finally{if(foregroundHook!=0)UnhookWinEvent(foregroundHook);GC.KeepAlive(callback);session?.Dispose();overlay?.Dispose();underneath.Close();File.WriteAllText(report,JsonSerializer.Serialize(new{result,observations},new JsonSerializerOptions{WriteIndented=true}));demo.Close();}
        };Application.Run(demo);return result;
    }
}




