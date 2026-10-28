namespace PipOverlay;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
#if !PRODUCTION
        if(args.Length>=2&&args[0]=="--contention-test")return ContentionTest.Run(args[1]);
        if(args.Length>=2&&args[0]=="--improvement-test")return ImprovementTests.Run(args[1]);
        if(args.Length>=2&&args[0]=="--quality-test")return QualityTests.Run(args[1]);
        if(args.Length>=3&&args[0]=="--multi-test")return MultiApiTests.Run(args[1],args[2],args.Length>3?args[3]:"--timed");
        if(args.Length>=2&&args[0]=="--geometry-test")return GeometryTests.Run(args[1]);
        if(args.Length>=2&&args[0]=="--ux-preview"){using var form=new MainForm(true);form.Show();Application.DoEvents();if(args.Contains("--advanced")){var all=new List<Control>();void Collect(Control c){all.Add(c);foreach(Control child in c.Controls)Collect(child);}Collect(form);all.OfType<Button>().First(x=>x.AccessibleName=="Mostrar ou ocultar ajustes avançados").PerformClick();Application.DoEvents();}using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(args[1]);return 0;}
        if(args.Length>=2&&args[0]=="--auto-test")return AutoConnectTests.Run(args[1]);
        if(args.Length>=2&&args[0]=="--hotkey-test")return HotkeyTests.Run(args[1],args.Contains("--native"));
        if(args.Length>=2 && args[0]=="--gpu-test") return GpuValidation.Run(args[1],args.Contains("--fullscreen"),args.Contains("--browser"));
        if(args.Length>=2 && args[0]=="--gpu-benchmark") return GpuValidation.Benchmark(args[1]);
        if(args.Length>=2 && args[0]=="--v4-test") return ValidationV4.Run(args[1]);
        if(args.Length>=2 && args[0]=="--position-probe") return ValidationV4.PositionProbe(args[1]);
        if(args.Length>=2 && args[0]=="--rtss-benchmark") return ValidationV4.Benchmark(args[1]);
        if(args.Length>=2 && args[0]=="--rtss-test") return RtssTest.Run(args[1]);
        if(args.Length>=2 && args[0]=="--rtss-probe") return RtssTest.Probe(args[1]);
        if(args.Length>=2 && args[0]=="--rtss-video-probe") return RtssTest.VideoProbe(args[1]);
        if(args.Length>=2 && args[0]=="--inspect") { Diagnostics.Inspect(args[1]);return 0; }
        if (args.Length>=2 && args[0]=="--self-test") return SelfTest.Run(args[1]);
#endif
        using var instance=new Mutex(true,"Local\\PipOverlay-Portable-0.1",out var first);
        if(!first) { MessageBox.Show("A ferramenta já está aberta. Dê dois cliques no ícone PiP Overlay perto do relógio para ajustar.","PiP Overlay");return 0; }
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        using var main=new MainForm();
        Application.Run(main);
        return 0;
    }
}
