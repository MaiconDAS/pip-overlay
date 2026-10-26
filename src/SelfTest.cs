namespace PipOverlay;

internal static class SelfTest
{
    internal static int Run(string report)
    {
        var results=new List<string>();var failed=0;
        void Check(string name,bool ok) { results.Add($"{(ok ? "PASS" : "FAIL")}  {name}");if(!ok) failed++; }
        bool Until(Func<bool> ready,int timeout=8000) {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            while(!ready() && watch.ElapsedMilliseconds<timeout) {Application.DoEvents();Thread.Sleep(15);}
            return ready();
        }
        try {
            var corrupt=new Settings { Width=-20,Height=99999,Opacity=300,X=-12,CropLeft=-10 }.Validated();
            Check("Valores inválidos são limitados",corrupt.Width==120 && corrupt.Height==4320 && corrupt.Opacity==100 && corrupt.X==0 && corrupt.CropLeft==0);
            var screen=new Rectangle(-1920,0,1920,1080);
            var bounds=new Settings { X=1800,Y=1000,Width=420,Height=236 }.Bounds(screen);
            Check("Monitor com coordenadas negativas; overlay permanece dentro da tela",bounds==new Rectangle(-420,844,420,236));
            var crop=new Settings { CropLeft=999,CropTop=999,CropRight=999,CropBottom=999 }.Crop(new Size(640,360));
            Check("Recortes extremos não produzem dimensões nulas/negativas",crop==new Rectangle(639,359,1,1));
            Check("Proporção 16:9 preservada em área quadrada",Settings.Fit(new Size(1920,1080),new Rectangle(0,0,400,400))==new Rectangle(0,87,400,225));
            var path=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!,"settings-test-"+Guid.NewGuid().ToString("N")+".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            try {
                var original=new Settings { X=200,Opacity=71,CropTop=32,Monitor="test" };
                original.Save(path);Check("Configurações salvas e lidas sem perda",Settings.Load(path)==original);
                File.WriteAllText(path,"{broken");Check("Arquivo inválido usa valores padrão",Settings.Load(path)==new Settings());
            } finally { if(File.Exists(path)) File.Delete(path); }

            // Integration fixture: every modified HWND belongs to this test process.
            using var source=new DemoSource {TestColor=Color.Lime};source.Show();source.Hide();source.Show();
            using var game=new Form { Text="PiP Overlay · jogo simulado",FormBorderStyle=FormBorderStyle.None,StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(420,80,700,500),BackColor=Color.FromArgb(40,50,70) };
            game.Show();game.Activate();Application.DoEvents();
            Native.SetWindowPos(game.Handle,Native.HwndTopmost,0,0,0,0,Native.NoMove|Native.NoSize|Native.NoActivatePos);
            Native.SetWindowPos(source.Handle,Native.HwndTopmost,0,0,0,0,Native.NoMove|Native.NoSize|Native.NoActivatePos);
            var info=new WindowSource(source.Handle,(uint)Environment.ProcessId,"Teste",source.Text,true);
            Check("Fonte de teste começa sempre no topo",Native.IsTopmost(source.Handle));
            var before=Native.GetForegroundWindow();
            using(var overlay=new Overlay()) {
                var placement=new Settings { X=450,Y=100,Width=360,Height=220 };
                overlay.Start(info,placement,Screen.PrimaryScreen!.Bounds);
                Application.DoEvents();Native.DwmFlush();
                var style=Native.GetWindowLongPtr(overlay.Handle,Native.GwlExStyle).ToInt64();
                var expected=Native.Layered|Native.Transparent|Native.NoActivate|Native.ToolWindow|Native.Topmost;
                Check("Overlay tem estilos de passagem de cliques, sem ativação e sem Alt+Tab",(style&expected)==expected);
                Check("Exibir overlay preserva o foco",Native.GetForegroundWindow()==before);
                Check("WGC entrega quadros reais (não apenas conexão)",Until(()=>overlay.CapturedFrames>=2));
                Check("Pixels da fonte verde chegam à overlay",Until(()=>overlay.CenterPixel is Color c && c.G>200 && c.R<40));
                using(var lease=new SourceLease(info)) {
                    Application.DoEvents();Native.DwmFlush();
                    var p=overlay.PointToScreen(new Point(70,70));
                    var hit=Native.WindowFromPoint(new Native.Point(p.X,p.Y));
                    Check($"Hit-test atravessa a overlay até a janela subjacente (hit={hit}; esperado={game.Handle})",hit==game.Handle);
                    Check("Fonte perde apenas a condição de sempre no topo",!Native.IsTopmost(source.Handle));
                    Check("Fonte continua aberta, visível e não minimizada",Native.IsWindowVisible(source.Handle)&&!Native.IsIconic(source.Handle));
                    Native.SetWindowPos(game.Handle,Native.HwndTopmost,0,0,0,0,Native.NoMove|Native.NoSize|Native.NoActivatePos);
                    overlay.Apply(placement,Screen.PrimaryScreen.Bounds);
                    source.TestColor=Color.Magenta;source.Invalidate();
                    Check("Vídeo continua atualizando com a fonte coberta por outra janela",Until(()=>overlay.CenterPixel is Color c && c.R>200 && c.B>200 && c.G<40));
                    Check("Overlay permanece visível e no topo após troca de foco",Native.IsWindowVisible(overlay.Handle)&&Native.IsTopmost(overlay.Handle));
                    Check("Atualização do vídeo preserva o foco original",Native.GetForegroundWindow()==before);
                    var resized=placement with { Width=500,Height=300,Opacity=65,CropTop=30 };
                    overlay.Apply(resized,Screen.PrimaryScreen.Bounds);Native.DwmFlush();
                    Check("Reposicionamento e recorte são aplicados",overlay.Width==500&&overlay.Height==300);
                    Check("Ajustes não roubam foco",Native.GetForegroundWindow()==before);
                }
                Check("Parar restaura a condição de sempre no topo",Native.IsTopmost(source.Handle));
            }
            Check("Fonte original permanece disponível após descarte da overlay",info.Exists);
            source.Close();Check("Fechamento da fonte é detectado",!info.Exists);
            game.Close();
        } catch(Exception ex) { failed++;results.Add("FAIL  "+ex); }
        results.Insert(0,$"PiP Overlay 0.2 — teste local de integração — {DateTimeOffset.Now:O}");
        results.Add($"Resultado: {failed} falha(s).");
        results.Add("Este teste não comprova reprodução de vídeo protegido, compatibilidade de jogos reais ou input físico.");
        File.WriteAllLines(report,results);
        return failed==0 ? 0 : 1;
    }
}
