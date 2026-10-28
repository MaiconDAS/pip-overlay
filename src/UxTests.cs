using System.Text.Json;
namespace PipOverlay;
internal static class UxTests
{
    internal static int Run(string report){
        var lines=new List<string>();int failures=0;void Check(string name,bool ok){lines.Add($"{(ok?"PASS":"FAIL")} {name}");if(!ok)failures++;}
        var screen=new Size(1920,1080);var a=new Rectangle(29,30,420,236);
        Check("Snap to margin",LayoutMath.Snap(a,screen,24,12).Location==new Point(24,24));
        Check("Snap center",LayoutMath.Snap(new(745,420,420,236),screen,24,12).Location==new Point(750,422));
        Check("Outside threshold stays free",LayoutMath.Snap(new(200,200,420,236),screen,24,12).Location==new Point(200,200));
        Check("Large overlay stays in screen",LayoutMath.Snap(new(0,0,1920,1080),screen,96,12)==new Rectangle(0,0,1920,1080));
        using var editor=new LayoutEditor{MonitorSize=screen,Video=new(100,100,640,360)};
        editor.Center();Check("Center preserves size",editor.Video==new Rectangle(640,360,640,360)&&editor.CanUndo);editor.Undo();Check("Undo restores prior layout",editor.Video==new Rectangle(100,100,640,360)&&!editor.CanUndo);
        editor.Center();editor.ClearHistory();Check("Profile switch clears undo",!editor.CanUndo);
        var old=JsonSerializer.Deserialize<GameProfile>("{\"Monitor\":\"\",\"X\":24,\"Y\":24,\"Width\":420,\"Height\":236,\"Resolution\":\"1080p\",\"Fps\":30,\"Adaptive\":false,\"MinimumFps\":15}")!;
        Check("Old profile receives snapping defaults",old.Apply(new Settings()) is {SnapEnabled:true,SnapMargin:24,FramesPerSecond:30});
        var profile=GameProfile.From(new Settings{SnapEnabled=false,SnapMargin=80,Resolution="720p",FramesPerSecond=30});Check("Profile copies snapping and quality",profile.Apply(new Settings()) is {SnapEnabled:false,SnapMargin:80,Resolution:"720p",FramesPerSecond:30});
        Check("Connection reasons distinct",new[]{ConnectionMessages.WaitingVideo,ConnectionMessages.WaitingGame,ConnectionMessages.Interrupted,ConnectionMessages.Unavailable}.Distinct().Count()==4);
        using var first=new Form{Text="Synthetic red source",BackColor=Color.Red,ClientSize=new Size(320,180),ShowInTaskbar=false};
        using var second=new Form{Text="Synthetic blue source",BackColor=Color.Blue,ClientSize=new Size(320,180),ShowInTaskbar=false};first.Show();second.Show();Application.DoEvents();
        var sources=new[]{new WindowSource(first.Handle,(uint)Environment.ProcessId,"Edge","Synthetic",true),new WindowSource(second.Handle,(uint)Environment.ProcessId,"Chrome","Synthetic",true)};
        using(var picker=new VideoPicker(sources)){picker.Show();Application.DoEvents();Check("Both thumbnail registrations",picker.ThumbnailCount==2);
            var buttons=picker.Controls[0].Controls.Cast<Control>().SelectMany(c=>c.Controls.OfType<Button>()).ToArray();buttons[1].PerformClick();Check("One click selects correct source",picker.Selected?.Handle==second.Handle);}
        using(var picker=new VideoPicker(sources)){picker.Show();Application.DoEvents();first.Close();var button=picker.Controls[0].Controls[0].Controls.OfType<Button>().First();button.PerformClick();Check("Closed source cannot be selected",picker.Selected==null&&!button.Enabled);picker.Close();}
        second.Close();File.WriteAllLines(report,lines);return failures==0?0:1;
    }
}
