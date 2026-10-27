namespace PipOverlay;
internal static class QualityTests
{
    internal static int Run(string report){
        var lines=new List<string>();int failures=0;void Check(string name,bool pass){lines.Add($"{(pass?"PASS":"FAIL")} {name}");if(!pass)failures++;}
        Check("Maximum allows 60 fps",new Settings().Validated().FramesPerSecond==60);
        Check("Balanced limits 30 fps",new Settings{Quality="balanced"}.Validated().FramesPerSecond==30);
        Check("Economy limits 15 fps",new Settings{Quality="economy"}.Validated().FramesPerSecond==15);
        Check("Unknown quality uses consistent maximum defaults",new Settings{Quality="invalid"}.Validated() is {Quality:"maximum",FramesPerSecond:60});
        using var source=new Form{Text="PiP quality test — temporary",ClientSize=new Size(640,360),StartPosition=FormStartPosition.Manual,Location=new Point(120,120),ShowInTaskbar=false};
        source.Show();Application.DoEvents();Native.GetWindowRect(source.Handle,out var before);
        var window=new WindowSource(source.Handle,(uint)Environment.ProcessId,"Test","Temporary",true);
        using(var lease=new SourceLease(window,new Size(1920,1080))){Application.DoEvents();Native.GetWindowRect(source.Handle,out var enlarged);Check("Capture source enlarges",enlarged.Right-enlarged.Left>before.Right-before.Left);Check("No topmost source",!Native.IsTopmost(source.Handle));}
        Application.DoEvents();Native.GetWindowRect(source.Handle,out var after);
        Check("Source bounds restored",before.Left==after.Left&&before.Top==after.Top&&before.Right==after.Right&&before.Bottom==after.Bottom);
        source.Close();File.WriteAllLines(report,lines);return failures==0?0:1;
    }
}
