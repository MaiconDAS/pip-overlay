namespace PipOverlay;

internal sealed class MainForm : Form
{
    readonly ComboBox sources=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=360};
    readonly ComboBox monitors=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=260};
    readonly ComboBox quality=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=165};
    readonly ComboBox targets=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=260};
    readonly LayoutEditor editor=new(){Dock=DockStyle.Fill};
    readonly Label status=new(){AutoSize=true,ForeColor=Color.Silver},dimensions=new(){AutoSize=true,ForeColor=Color.Silver};
    readonly Button start=new(),stop=new();
    readonly System.Windows.Forms.Timer pulse=new(){Interval=200};
    readonly System.Windows.Forms.Timer save=new(){Interval=600};
    readonly System.Windows.Forms.Timer discovery=new(){Interval=1500};
    readonly AutoConnect automatic=new();
    readonly NotifyIcon tray=new();
    bool refreshing,exiting,connectionFailed;
    readonly bool designPreviewMode;
    protected override bool ShowWithoutActivation=>designPreviewMode;
    int? activeTarget;
    readonly string settingsPath=Path.Combine(AppContext.BaseDirectory,"data","settings.json");
    Settings settings;
    GpuVideoSession? session;
    SourceLease? lease;
    WindowSource? active;
    Hotkeys? hotkeys;
    bool configuringShortcuts;
    ShortcutDialog? shortcutDialog;

    long lastFrames,lastTick;
    readonly System.Diagnostics.Stopwatch elapsed=new();
    internal MainForm(bool designPreview=false)
    {
        designPreviewMode=designPreview;
        settings=Settings.Load(settingsPath) with {Opacity=100,CropLeft=0,CropTop=0,CropRight=0,CropBottom=0};
        if(!settings.AutomaticEnabled)automatic.Pause();
        Text="PiP Overlay";Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(15,20,30);ForeColor=Color.White;
        Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize=new Size(1140,780);MinimumSize=new Size(1040,720);StartPosition=FormStartPosition.CenterScreen;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=6};Controls.Add(layout);
        foreach(int h in new[]{90,82,78,38})layout.RowStyles.Add(new(SizeType.Absolute,h));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,72));
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};header.ColumnStyles.Add(new(SizeType.Absolute,64));header.ColumnStyles.Add(new(SizeType.Percent,100));
        header.Controls.Add(new PictureBox{Image=UiTheme.Icon("pip",UiTheme.Accent,44),Size=new Size(48,48),Margin=new Padding(0,6,0,0)});
        var heading=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};heading.Controls.Add(new Label{Text="PiP Overlay",AutoSize=true,Font=new Font("Segoe UI Semibold",24),Margin=Padding.Empty});heading.Controls.Add(new Label{Text="Seu vídeo por perto. Seu jogo em foco.",AutoSize=true,ForeColor=UiTheme.Muted,Margin=new Padding(0,5,0,0)});header.Controls.Add(heading);layout.Controls.Add(header,0,0);
        foreach(var box in new[]{sources,targets,monitors,quality})UiTheme.Combo(box);
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};toolbar.Controls.Add(UiTheme.Field("VÍDEO DO NAVEGADOR",sources,360));toolbar.Controls.Add(UiTheme.Field("JOGO",targets,300));
        var refresh=new Button{AccessibleName="Atualizar vídeos e jogos"};UiTheme.Button(refresh,"Atualizar","refresh");refresh.Margin=new Padding(0,25,0,0);refresh.Click+=(_,_)=>{automatic.Reset();Discover();};toolbar.Controls.Add(refresh);layout.Controls.Add(toolbar,0,1);
        quality.Items.AddRange(new object[]{"Fluido · 30 fps","Econômico · 15 fps"});quality.SelectedIndex=settings.FramesPerSecond==15?1:0;
        var options=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};options.Controls.Add(UiTheme.Field("MONITOR",monitors,260));options.Controls.Add(UiTheme.Field("REPRODUÇÃO",quality,205));dimensions.Margin=new Padding(10,31,0,0);dimensions.ForeColor=UiTheme.Muted;options.Controls.Add(dimensions);layout.Controls.Add(options,0,2);
        layout.Controls.Add(new Label{Text="POSIÇÃO DO VÍDEO    ·    Arraste para mover. Puxe as bordas para redimensionar.",AutoSize=true,ForeColor=UiTheme.Muted,Margin=new Padding(0,12,0,0)},0,3);layout.Controls.Add(editor,0,4);
        var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,16,0,0)};UiTheme.Button(start,"Retomar","play",true);UiTheme.Button(stop,"Pausar","stop");status.AutoSize=false;status.Size=new Size(650,44);status.TextAlign=ContentAlignment.MiddleLeft;status.ForeColor=UiTheme.Muted;footer.Controls.Add(start);footer.Controls.Add(stop);footer.Controls.Add(status);layout.Controls.Add(footer,0,5);
        toolbar.Visible=options.Visible=false;layout.RowStyles[1].Height=0;layout.RowStyles[2].Height=0;
        var advanced=new Button{AccessibleName="Mostrar ou ocultar ajustes avançados"};UiTheme.Button(advanced,"Ajustes","pip");advanced.Margin=new Padding(0,8,0,0);header.ColumnCount=3;header.ColumnStyles.Add(new(SizeType.Absolute,160));header.Controls.Add(advanced,2,0);
        advanced.Click+=(_,_)=>{bool show=!toolbar.Visible;toolbar.Visible=options.Visible=show;layout.RowStyles[1].Height=show?82:0;layout.RowStyles[2].Height=show?78:0;advanced.Text=show?"Ocultar":"Ajustes";};
        foreach(var screen in Screen.AllScreens)monitors.Items.Add(new MonitorItem(screen));monitors.SelectedIndex=0;
        for(int i=0;i<monitors.Items.Count;i++)if(((MonitorItem)monitors.Items[i]!).Screen.DeviceName==settings.Monitor)monitors.SelectedIndex=i;
        UpdateCanvas();if(!designPreview){RefreshSources();RefreshTargets();}
        editor.LayoutChanged+=box=>{settings=settings with {X=box.X,Y=box.Y,Width=box.Width,Height=box.Height};Apply();};
        monitors.SelectedIndexChanged+=(_,_)=>{Stop("");automatic.Reset();UpdateCanvas();Discover();};
        sources.SelectedIndexChanged+=(_,_)=>{if(refreshing)return;Stop("");automatic.Reset();PreviewSelection();Discover();};
        quality.SelectedIndexChanged+=(_,_)=>{settings=settings with {FramesPerSecond=quality.SelectedIndex==1?15:30};Apply();};
        start.Click+=(_,_)=>ResumeAutomatic();stop.Click+=(_,_)=>PauseAutomatic();
        pulse.Tick+=(_,_)=>Poll();save.Tick+=(_,_)=>{save.Stop();Save();};
        targets.SelectedIndexChanged+=(_,_)=>{if(refreshing)return;Stop("");automatic.Reset();Discover();};
        FormClosing+=(_,e)=>{if(!exiting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();tray.ShowBalloonTip(2500,"PiP Overlay continua ativo","Use o ícone perto do relógio para ajustar, pausar ou sair.",ToolTipIcon.Info);return;}Stop("");Save();};
        PreviewSelection();UpdateActions();status.Text="Abra o PiP no Edge ou Chrome. A overlay inicia sozinha.";
        discovery.Tick+=(_,_)=>Discover();if(!designPreview)discovery.Start();
        tray.Icon=Icon;tray.Text="PiP Overlay · automático";tray.Visible=!designPreview;
        VisibleChanged+=(_,_)=>{if(!Visible)editor.DetachSource();else PreviewSelection();};
        var menu=new ContextMenuStrip();menu.Items.Add("Ajustar posição e tamanho",null,(_,_)=>ShowEditor());menu.Items.Add("Retomar",null,(_,_)=>ResumeAutomatic());menu.Items.Add("Pausar",null,(_,_)=>PauseAutomatic());menu.Items.Add("Sair",null,(_,_)=>{exiting=true;Close();});tray.ContextMenuStrip=menu;tray.DoubleClick+=(_,_)=>ShowEditor();
        var shortcuts=new Button{AccessibleName="Configurar atalhos de teclado"};UiTheme.Button(shortcuts,"Atalhos","keyboard");shortcuts.Margin=new Padding(12,25,0,0);toolbar.Controls.Add(shortcuts);shortcuts.Click+=(_,_)=>ConfigureShortcuts();
        Shown+=(_,_)=>{if(designPreview)return;hotkeys=new Hotkeys(Handle);Discover();if(!hotkeys.Apply(new(settings.StartShortcut),new(settings.StopShortcut),out var error))status.Text=error;};
        if(designPreview){save.Stop();exiting=true;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(-20000,-20000);}
    }
    void ConfigureShortcuts(){if(hotkeys==null)return;configuringShortcuts=true;try{using var dialog=new ShortcutDialog(new(settings.StartShortcut),new(settings.StopShortcut),(a,b)=>{if(!hotkeys.Apply(a,b,out var error))return error;settings=settings with{StartShortcut=a.Value,StopShortcut=b.Value};Save();return null;});shortcutDialog=dialog;dialog.ShowDialog(this);}finally{shortcutDialog=null;configuringShortcuts=false;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x312&&hotkeys!=null){if(configuringShortcuts)shortcutDialog?.ReceiveRegisteredShortcut(hotkeys.ShortcutFor((int)m.WParam));else{int action=hotkeys.ActionFor((int)m.WParam);if(action==0)ResumeAutomatic();else if(action==1)PauseAutomatic();}return;}base.WndProc(ref m);}
    void ShowEditor(){Show();WindowState=FormWindowState.Normal;Activate();}
    void UpdateActions(){start.Visible=!automatic.Enabled||connectionFailed;start.Text=connectionFailed?"Tentar novamente":"Retomar";start.Enabled=true;stop.Visible=automatic.Enabled;stop.Enabled=true;}
    void PauseAutomatic(){automatic.Pause();connectionFailed=false;settings.AutomaticEnabled=false;Stop("Pausado · clique em Retomar quando quiser.");PreviewSelection();Save();}
    void ResumeAutomatic(){automatic.Resume();connectionFailed=false;settings.AutomaticEnabled=true;Save();Discover();}
    void Fail(string message){connectionFailed=true;Stop(message);}
    void Discover(){
        if(configuringShortcuts||refreshing)return;
        if(session!=null){if(activeTarget is int pid){try{using var p=System.Diagnostics.Process.GetProcessById(pid);if(!p.HasExited)return;}catch(ArgumentException){}}Stop("Aguardando o jogo abrir…");}
        refreshing=true;
        try{
            var found=WindowSource.Find().Where(x=>x.LikelyPip).ToArray();
            var games=RenderTarget.Find().Where(x=>x.Name=="DigimonMasters").ToArray();
            if(!found.Select(x=>(x.Handle,x.ProcessId)).SequenceEqual(sources.Items.Cast<WindowSource>().Select(x=>(x.Handle,x.ProcessId)))){var old=sources.SelectedItem as WindowSource;sources.Items.Clear();sources.Items.AddRange(found);sources.SelectedItem=found.FirstOrDefault(x=>x.Handle==old?.Handle&&x.ProcessId==old?.ProcessId)??found.FirstOrDefault();PreviewSelection();}
            if(!games.Select(x=>x.Pid).SequenceEqual(targets.Items.Cast<RenderTarget>().Select(x=>x.Pid))){var old=targets.SelectedItem as RenderTarget;targets.Items.Clear();targets.Items.AddRange(games);targets.SelectedItem=games.FirstOrDefault(x=>x.Pid==old?.Pid)??games.FirstOrDefault();}
            var source=sources.SelectedItem as WindowSource;var target=targets.SelectedItem as RenderTarget;
            UpdateActions();tray.Text=automatic.Enabled?"PiP Overlay · automático":"PiP Overlay · pausado";
            if(automatic.TakeAttempt(source==null?null:$"{source.Handle}:{source.ProcessId}",target?.Pid,false))Start();
            else if(!automatic.Enabled)status.Text="Pausado · clique em Retomar quando quiser.";
            else if(source==null)status.Text="Abra o vídeo em PiP no Edge ou Chrome.";
            else if(target==null)status.Text="Vídeo pronto · aguardando seu jogo abrir.";
        }finally{refreshing=false;}
    }
    protected override void OnHandleDestroyed(EventArgs e){hotkeys?.Dispose();hotkeys=null;base.OnHandleDestroyed(e);}
    Screen SelectedScreen=>((MonitorItem?)monitors.SelectedItem)?.Screen??Screen.AllScreens[0];
    void UpdateCanvas(){editor.MonitorSize=SelectedScreen.Bounds.Size;settings=settings with {Monitor=SelectedScreen.DeviceName};var b=LayoutMath.Constrain(new(settings.X,settings.Y,settings.Width,settings.Height),editor.MonitorSize,editor.Aspect);settings=settings with{X=b.X,Y=b.Y,Width=b.Width,Height=b.Height};editor.Video=b;Apply();}
    void Apply(){editor.Video=new(settings.X,settings.Y,settings.Width,settings.Height);editor.Invalidate();session?.Apply(settings);
        dimensions.Text=$"Vídeo · {100.0*settings.Width/editor.MonitorSize.Width:F0}% da largura da tela";
        save.Stop();save.Start();}
    void Save(){try{settings.Save(settingsPath);}catch(Exception e)when(e is IOException or UnauthorizedAccessException){status.Text="Não foi possível salvar: "+e.Message;}}
    void RefreshSources(){Stop("");sources.Items.Clear();foreach(var source in WindowSource.Find().Where(x=>x.LikelyPip))sources.Items.Add(source);if(sources.Items.Count>0)sources.SelectedIndex=0;else status.Text="Abra o PiP do navegador. Vídeo e jogo são detectados automaticamente.";}
    void PreviewSelection(){if(sources.SelectedItem is WindowSource source&&source.Exists){Native.GetWindowRect(source.Handle,out var rect);if(rect.Right>rect.Left&&rect.Bottom>rect.Top){editor.Aspect=(double)(rect.Right-rect.Left)/(rect.Bottom-rect.Top);UpdateCanvas();}editor.AttachSource(source.Handle);}}
    void RefreshTargets(){Stop("");targets.Items.Clear();targets.Items.AddRange(RenderTarget.Find().Where(x=>x.Name=="DigimonMasters").ToArray());if(targets.Items.Count>0)targets.SelectedIndex=0;}
    void Start(){if(sources.SelectedItem is not WindowSource source){status.Text="Abra o vídeo em PiP no Edge ou Chrome.";return;}connectionFailed=false;Stop("");try{
        if(!source.Exists||Native.IsIconic(source.Handle))throw new InvalidOperationException("Restaure o PiP antes de iniciar.");
        if(targets.SelectedItem is not RenderTarget target)throw new InvalidOperationException("Abra seu jogo e clique em Atualizar.");
        session=new GpuVideoSession(source,settings,editor.MonitorSize,target);editor.Aspect=(double)session.SourceSize.Width/session.SourceSize.Height;UpdateCanvas();editor.AttachSource(source.Handle);
        lease=new SourceLease(source);active=source;activeTarget=target.Pid;start.Enabled=false;stop.Enabled=true;elapsed.Restart();lastFrames=0;lastTick=Environment.TickCount64;pulse.Start();status.Text="Conectando ao vídeo…";
    }catch(Exception e){Fail(e.Message);}}
    void Stop(string message){pulse.Stop();session?.Dispose();session=null;editor.DetachSource();lease?.Dispose();lease=null;active=null;activeTarget=null;UpdateActions();status.Text=message;}
    void Poll(){if(session==null||active==null)return;
        if(!active.Exists){Stop("Abra outro vídeo em PiP no navegador.");automatic.Reset();return;}
        if(session.Error is string error){Fail(error);return;}
        if(session.Published==0&&elapsed.Elapsed.TotalSeconds>8){Fail("Nenhum quadro recebido. Restaure o PiP.");return;}
        if(!session.Ready&&elapsed.Elapsed.TotalSeconds>8){Fail("O renderizador Direct3D não confirmou a conexão.");return;}
        if(Visible&&WindowState!=FormWindowState.Minimized)editor.RefreshThumbnail();lease?.KeepBehind();
        long now=Environment.TickCount64;if(now-lastTick>=1000){long count=session.Published;double fps=(count-lastFrames)*1000.0/(now-lastTick);lastFrames=count;lastTick=now;status.Text=session.Rendered>0?$"●  Overlay ativa · {fps:F0} fps":"Aguardando o jogo ficar visível…";}
    }
    protected override void Dispose(bool disposing){if(disposing){tray.Visible=false;tray.ContextMenuStrip?.Dispose();tray.Dispose();discovery.Dispose();pulse.Dispose();save.Dispose();session?.Dispose();session=null;lease?.Dispose();lease=null;}base.Dispose(disposing);}
    sealed record MonitorItem(Screen Screen){public override string ToString()=>$"{Screen.DeviceName.Replace("\\\\.\\","")} · {Screen.Bounds.Width}×{Screen.Bounds.Height}";}
}
