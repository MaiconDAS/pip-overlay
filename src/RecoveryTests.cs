using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
namespace PipOverlay;

internal static class RecoveryTests
{
    sealed class Response(byte[] data):HttpMessageHandler{
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){token.ThrowIfCancellationRequested();return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(data)});}
    }
    internal static int Run(string report){
        var lines=new List<string>();int failed=0;
        void Check(string name,bool ok){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failed++;}
        string temp=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!,"recovery-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try{
            string root=Path.Combine(temp,"data"),config=Path.Combine(root,"settings.json"),archive=Path.Combine(temp,"export.json");
            var initial=new Settings{X=120,Width=960,Height=540,GameExecutable="private-local-path",StartShortcut=123,Language="de"};initial.Save(config);
            var store=new ProfileStore(Path.Combine(root,"profiles"));string key=ProfileStore.Key(Path.Combine(temp,"Game.exe"));store.Save(key,initial);
            PreferencesArchive.Export(root,archive);Check("Export excludes executable path",!File.ReadAllText(archive).Contains("private-local-path"));
            (initial with{X=300,GameExecutable="retain-local-path"}).Save(config);string backup=PreferencesArchive.Import(root,archive);var imported=Settings.Load(config);
            Check("Import restores preferences and pauses",imported.X==120&&imported.Language=="de"&&!imported.AutomaticEnabled);
            Check("Import preserves local executable",imported.GameExecutable=="retain-local-path");
            Check("Pre-import backup is readable",PreferencesArchive.Read(backup).Settings.X==300);
            string invalid=Path.Combine(temp,"invalid.json");File.WriteAllText(invalid,JsonSerializer.Serialize(new PreferencesBundle(1,initial,new(){["../escape"]=GameProfile.From(initial)})));
            string before=File.ReadAllText(config);try{PreferencesArchive.Import(root,invalid);Check("Reject traversal",false);}catch(InvalidDataException){Check("Reject traversal without changes",File.ReadAllText(config)==before);}
            File.WriteAllBytes(invalid,new byte[4*1024*1024+1]);try{PreferencesArchive.Read(invalid);Check("Reject oversized archive",false);}catch(InvalidDataException){Check("Reject oversized archive",true);}
            store.Save(key,initial with{X=333});
            using(var locked=new FileStream(config+".tmp",FileMode.Create,FileAccess.Write,FileShare.None)){
                try{PreferencesArchive.Import(root,archive);Check("Rollback on write failure",false);}catch(IOException){Check("Rollback on write failure",File.ReadAllText(config)==before&&store.Load(key)!.X==333);}
            }
            File.WriteAllText(config,"{\"PreferDesktop\":false,\"FramesPerSecond\":30}");Check("Old settings migrate to independent output",Settings.Load(config).PreferDesktop);
            new Settings{PreferDesktop=false}.Save(config);Check("Explicit advanced choice persists",!Settings.Load(config).PreferDesktop);
            var layout=new DisplayLayout(1920,1080,1440,810,480,270);var scaled=layout.Fit(new(3840,2160));
            Check("Resolution scaling keeps size ratio and edges",scaled==new Rectangle(2880,1620,960,540));
            var portrait=layout.Fit(new(1080,1920));Check("Portrait fits and preserves aspect",portrait.Right==1080&&portrait.Bottom==1920&&Math.Abs(portrait.Width/(double)portrait.Height-16d/9)<.02);
            var saved=initial with{Monitor="A",PreferredMonitor="B",MonitorLayouts=new(){["A"]=layout,["B"]=new(2560,1440,0,0,640,360)}};
            store.Save(key,saved);var restored=store.Load(key)!.Apply(new Settings{Language="en",StartShortcut=456});
            Check("Per-monitor game layouts preserve global preferences",restored.MonitorLayouts.Count==2&&restored.PreferredMonitor=="B"&&restored.Language=="en"&&restored.StartShortcut==456);
            restored.MonitorLayouts["A"]=new(800,600,0,0,200,113);Check("Profiles do not share mutable dictionaries",saved.MonitorLayouts["A"]==layout);
            using(var editor=new LayoutEditor{MonitorSize=new(1920,1080),Video=new(0,0,480,270)}){
                editor.AdjustByKeyboard(Keys.Right,false,true);Check("Keyboard moves ten pixels with Shift",editor.Video.X==10);
                editor.Undo();Check("Keyboard undo",editor.Video.X==0);editor.AdjustByKeyboard(Keys.Right,true,true);Check("Keyboard resize preserves aspect",editor.Video.Width==490&&Math.Abs(editor.Video.Width/(double)editor.Video.Height-16d/9)<.02);
                editor.AdjustByKeyboard(Keys.Up,false,true);Check("Keyboard cannot move off screen",editor.Video.Y==0);
            }
            byte[] payload=System.Text.Encoding.UTF8.GetBytes("synthetic installer, never executed");string digest=Convert.ToHexString(SHA256.HashData(payload));
            const string url="https://github.com/MaiconDAS/pip-overlay/releases/download/v1.2.3/PiP-Overlay-v1.2.3.exe";
            var release=new InstallerRelease(new(1,2,3),url,"PiP-Overlay-v1.2.3.exe",digest,payload.Length);
            string downloads=Path.Combine(temp,"downloads");using var client=new HttpClient(new Response(payload));
            string downloaded=VerifiedUpdates.Download(client,release,downloads,null,CancellationToken.None).GetAwaiter().GetResult();Check("Verified download matches payload",File.ReadAllBytes(downloaded).SequenceEqual(payload));
            try{VerifiedUpdates.Download(client,release with{Sha256=new string('0',64)},downloads,null,CancellationToken.None).GetAwaiter().GetResult();Check("Reject hash mismatch",false);}catch(InvalidDataException){Check("Hash failure preserves previous verified file",File.ReadAllBytes(downloaded).SequenceEqual(payload)&&!Directory.EnumerateFiles(downloads,"*.partial").Any());}
            using(var cancel=new CancellationTokenSource()){cancel.Cancel();try{VerifiedUpdates.Download(client,release,downloads,null,cancel.Token).GetAwaiter().GetResult();Check("Cancellation",false);}catch(OperationCanceledException){Check("Cancellation leaves no partial files",!Directory.EnumerateFiles(downloads,"*.partial").Any());}}
            try{VerifiedUpdates.Download(client,release with{Url="https://evil.example/file"},downloads,null,CancellationToken.None).GetAwaiter().GetResult();Check("Reject other repository",false);}catch(InvalidDataException){Check("Reject other repository",true);}
            string Releases(bool pre,string assetUrl)=>JsonSerializer.Serialize(new[]{new{draft=false,prerelease=pre,tag_name="v1.2.3",html_url="https://github.com/MaiconDAS/pip-overlay/releases/tag/v1.2.3",assets=new[]{new{name=release.Name,browser_download_url=assetUrl,digest="sha256:"+digest,size=payload.Length}}}});
            Check("Stable release has verified metadata",VerifiedUpdates.Parse(Releases(false,url)).Length==1);
            Check("Prerelease not offered",VerifiedUpdates.Parse(Releases(true,url)).Length==0&&UpdateService.Parse(Releases(true,url),new(0,1,0))==null);
            Check("Foreign asset not offered",VerifiedUpdates.Parse(Releases(false,"https://evil.example/file")).Length==0);
            Check("Diagnostics keep private fields out",!SupportReport.Create(initial,null,0,true).Contains("private-local-path"));
            L.Set("pt-BR");using(var form=new MainForm(true)){
                form.Show();Application.DoEvents();
                IEnumerable<Control> Walk(Control c){yield return c;foreach(Control child in c.Controls)foreach(var nested in Walk(child))yield return nested;}
                var controls=Walk(form).ToArray();Check("Video picker remains available",controls.OfType<Button>().Single(x=>x.Text=="Vídeos").Visible);
                controls.OfType<Button>().Single(x=>x.Text=="Ajustes").PerformClick();controls.OfType<Button>().Single(x=>x.Text=="Mais").PerformClick();Application.DoEvents();
                var direct=controls.OfType<ModernToggle>().Single(x=>x.Text=="Conectar diretamente ao jogo");
                var target=controls.OfType<ComboBox>().Single(x=>x.AccessibleName=="CONEXÃO AVANÇADA");
                Check("Default desktop hides process choice",!direct.Checked&&!target.Visible);
                direct.Checked=true;Application.DoEvents();Check("Direct connection requires explicit opt-in",target.Visible);
                direct.Checked=false;Application.DoEvents();Check("Desktop offers only independent target",target.Items.Count==1&&target.Items[0] is RenderTarget {Backend:"desktop"});
                form.Size=form.MinimumSize;Application.DoEvents();
                Check("Settings do not overflow horizontally",!controls.OfType<FlowLayoutPanel>().Any(x=>x.Visible&&x.AutoScroll&&x.HorizontalScroll.Visible));
                form.Close();
            }
        }catch(Exception ex){Check(ex.ToString(),false);}
        finally{foreach(string file in Directory.EnumerateFiles(temp,"*",SearchOption.AllDirectories))File.Delete(file);foreach(string dir in Directory.EnumerateDirectories(temp,"*",SearchOption.AllDirectories).OrderByDescending(x=>x.Length))Directory.Delete(dir);Directory.Delete(temp);}
        File.WriteAllLines(report,lines);return failed==0?0:1;
    }
}
