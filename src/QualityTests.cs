namespace PipOverlay;
internal static class QualityTests
{
    internal static int Run(string report){
        var lines=new List<string>();int failures=0;void Check(string name,bool pass){lines.Add($"{(pass?"PASS":"FAIL")} {name}");if(!pass)failures++;}
        foreach(var resolution in new[]{"1080p","720p","480p","auto"})foreach(int fps in new[]{15,30,60}){
            var value=new Settings{Resolution=resolution,FramesPerSecond=fps,Quality="economy"}.Validated();
            var expected=resolution switch{"1080p"=>new Size(1920,1080),"720p"=>new Size(1280,720),"480p"=>new Size(854,480),_=>new Size(448,252)};
            Check($"Independent {resolution} / {fps}",value.Resolution==resolution&&value.FramesPerSecond==fps&&value.CaptureResolution==expected);
        }
        Check("Invalid values recover",new Settings{Resolution="invalid",FramesPerSecond=0}.Validated() is {Resolution:"1080p",FramesPerSecond:30});
        var legacy=System.Text.Json.JsonSerializer.Deserialize<Settings>("{\"Quality\":\"balanced\",\"FramesPerSecond\":30}")!.Validated();
        Check("Legacy balanced preserves 30 fps and adaptive resolution",legacy is {Resolution:"auto",FramesPerSecond:30});
        Check("Legacy maximum preserves resolution",new Settings{Quality="maximum",FramesPerSecond=60}.Validated() is {Resolution:"1080p",FramesPerSecond:60});
        string folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!,"settings-test-"+Guid.NewGuid().ToString("N"));
        string path=Path.Combine(folder,"settings.json");
        new Settings{Resolution="1080p",FramesPerSecond=30}.Save(path);
        Check("1080p30 persists",Settings.Load(path) is {Resolution:"1080p",FramesPerSecond:30});
        File.Delete(path);Directory.Delete(folder);
        using var source=new Form{Text="PiP quality test — temporary",ClientSize=new Size(640,360),StartPosition=FormStartPosition.Manual,Location=new Point(120,120),ShowInTaskbar=false};
        source.Show();Application.DoEvents();Native.GetWindowRect(source.Handle,out var before);
        var window=new WindowSource(source.Handle,(uint)Environment.ProcessId,"Test","Temporary",true);
        using(var lease=new SourceLease(window,new Size(1920,1080))){Application.DoEvents();Native.GetWindowRect(source.Handle,out var enlarged);Check("Capture source enlarges",enlarged.Right-enlarged.Left>before.Right-before.Left);Check("No topmost source",!Native.IsTopmost(source.Handle));}
        Application.DoEvents();Native.GetWindowRect(source.Handle,out var after);
        Check("Source bounds restored",before.Left==after.Left&&before.Top==after.Top&&before.Right==after.Right&&before.Bottom==after.Bottom);
        source.Close();File.WriteAllLines(report,lines);return failures==0?0:1;
    }
}
