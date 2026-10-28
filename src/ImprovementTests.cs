using System.Text.Json;
namespace PipOverlay;
internal static class ImprovementTests
{
    internal static int Run(string report){
        var lines=new List<string>();int failures=0;void Check(string name,bool ok){lines.Add($"{(ok?"PASS":"FAIL")} {name}");if(!ok)failures++;}
        var adaptive=new AdaptiveFrameRate();adaptive.Configure(true,15,60);for(int i=0;i<3;i++)adaptive.Sample(20);Check("Overload reduces 60 to 30",adaptive.Current==30);
        for(int i=0;i<3;i++)adaptive.Sample(40);Check("Overload respects floor 15",adaptive.Current==15);
        for(int i=0;i<19;i++)adaptive.Sample(1);Check("Recovery waits",adaptive.Current==15);adaptive.Sample(1);Check("Recovery raises one step",adaptive.Current==30);
        adaptive.Configure(true,30,60);for(int i=0;i<20;i++)adaptive.Sample(100);Check("User floor 30 respected",adaptive.Current==30);
        adaptive.Configure(false,15,60);for(int i=0;i<20;i++)adaptive.Sample(100);Check("Manual rate unchanged",adaptive.Current==60);
        adaptive.Configure(true,30,15);for(int i=0;i<3;i++)adaptive.Sample(100);Check("Minimum never exceeds maximum",adaptive.Current==15);
        string folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!,"profiles-test-"+Guid.NewGuid().ToString("N"));var store=new ProfileStore(folder);
        string key=ProfileStore.Key(Path.Combine(folder,"Game.exe")),other=ProfileStore.Key(Path.Combine(folder,"Other.exe"));
        var source=new Settings{X=100,Width=800,Resolution="1080p",FramesPerSecond=30,AdaptiveFps=true,StartShortcut=123,GameExecutable="private-secret"};store.Save(key,source);
        var loaded=store.Load(key)!.Apply(new Settings{StartShortcut=456,GameExecutable="other-secret"});Check("Profile restores layout and quality",loaded.X==100&&loaded.Width==800&&loaded.Resolution=="1080p"&&loaded.FramesPerSecond==30&&loaded.AdaptiveFps);
        Check("Profile excludes global shortcuts and executable",loaded.StartShortcut==456&&loaded.GameExecutable=="other-secret");Check("Games isolated",store.Load(other)==null);
        File.WriteAllText(Path.Combine(folder,other+".json"),"broken");Check("Corrupt profile ignored",store.Load(other)==null);
        string clean=SupportReport.Create(source,null);Check("Diagnostic excludes private fields",!clean.Contains("private-secret")&&!clean.Contains("GameExecutable")&&!clean.Contains("StartShortcut"));
        string releases=JsonSerializer.Serialize(new[]{new{draft=false,tag_name="v0.13.0",html_url="https://github.com/MaiconDAS/pip-overlay/releases/tag/v0.13.0"},new{draft=false,tag_name="v9.0.0",html_url="https://evil.example/file"},new{draft=true,tag_name="v9.0.0",html_url="https://github.com/MaiconDAS/pip-overlay/releases/tag/v9.0.0"}});
        Check("Update accepts only project releases",UpdateService.Parse(releases,new Version(0,12,0))?.Version==new Version(0,13,0));Check("Malformed entries ignored",UpdateService.Parse("[{},null,{\"draft\":true}]",new Version(0,12,0))==null);Check("No downgrade",UpdateService.Parse(releases,new Version(0,13,0))==null);
        foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);
        File.WriteAllLines(report,lines);return failures==0?0:1;
    }
}
