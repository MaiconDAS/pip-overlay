namespace PipOverlay;

internal sealed partial class MainForm : Form
{
    readonly ModernComboBox sources=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=360};
    readonly ModernComboBox monitors=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=220};
    readonly ModernComboBox quality=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=180};
    readonly ModernComboBox frameRate=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=90};
    readonly ModernComboBox targets=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=260};
    readonly LayoutEditor editor=new(){Dock=DockStyle.Fill};
    readonly Label status=new(){AutoSize=true,ForeColor=Color.Silver},dimensions=new(){AutoSize=true,ForeColor=Color.Silver};
    readonly ModernButton start=new(),stop=new(),pickVideo=new(),undoLayout=new(),centerLayout=new();
    readonly Label profileLabel=new(){Text=L.T("Perfil: Geral"),AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft,Width=420,Height=42,ForeColor=UiTheme.Muted};
    readonly ModernToggle snapBox=new(){Text=L.T("Encaixar nas bordas"),AutoSize=true};
    readonly ModernSlider margin=new(){Minimum=0,Maximum=96,TickFrequency=24,Width=150,Height=45};
    readonly Label marginLabel=new(){Width=110,Height=40};
    bool pickingVideo;long lastVideoRefresh;
    readonly System.Windows.Forms.Timer pulse=new(){Interval=200};
    readonly System.Windows.Forms.Timer save=new(){Interval=600};
    readonly System.Windows.Forms.Timer discovery=new(){Interval=1500};
    readonly AutoConnect automatic=new();
    readonly NotifyIcon tray=new();
    bool refreshing,exiting,connectionFailed,syncing,preferencesImported;
    readonly ModernToggle adaptiveBox=new(){Text=L.T("FPS automático"),AutoSize=true};
    readonly ModernComboBox minimumFps=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=80};
    readonly ModernToggle updateChecks=new(){Text=L.T("Avisar atualizações"),AutoSize=true};
    readonly ModernButton updates=new(),diagnostic=new();
    readonly ProfileStore profiles=new(Path.Combine(AppContext.BaseDirectory,"data","profiles"));
    string profileName="";string? profileKey;AppRelease? availableRelease;bool checkingUpdate;
    readonly bool designPreviewMode;
    protected override bool ShowWithoutActivation=>designPreviewMode;
    int? activeTarget;
    readonly string settingsPath=Path.Combine(AppContext.BaseDirectory,"data","settings.json");
    Settings settings;
    GpuVideoSession? session;
    DesktopOverlay? desktopOverlay;
    readonly HashSet<int> failedTargets=new();
    SourceLease? lease;
    WindowSource? active;
    Hotkeys? hotkeys;
    WindowEvents? windowEvents;
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
        ClientSize=new Size(1140,780);MinimumSize=new Size(920,620);StartPosition=FormStartPosition.CenterScreen;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=7};Controls.Add(layout);
        foreach(int h in new[]{90,82,78,130,54})layout.RowStyles.Add(new(SizeType.Absolute,h));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,72));
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};header.ColumnStyles.Add(new(SizeType.Absolute,64));header.ColumnStyles.Add(new(SizeType.Percent,100));
        header.Controls.Add(new PictureBox{Image=UiTheme.Brand(48),SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(48,48),Margin=new Padding(0,6,0,0)});
        var heading=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};heading.Controls.Add(new Label{Text="PiP Overlay",AutoSize=true,Font=new Font("Segoe UI Semibold",24),Margin=Padding.Empty});heading.Controls.Add(new Label{Text=L.T("Seu vídeo por perto. Seu jogo em foco."),AutoSize=true,ForeColor=UiTheme.Muted,Margin=new Padding(0,5,0,0)});header.Controls.Add(heading);layout.Controls.Add(header,0,0);
        foreach(var box in new[]{sources,targets,monitors,quality,frameRate})UiTheme.Combo(box);
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};toolbar.Controls.Add(UiTheme.Field(L.T("VÍDEO DO NAVEGADOR"),sources,360));toolbar.Controls.Add(UiTheme.Field(L.T("JOGO"),targets,300));
        var refresh=new ModernButton{AccessibleName=L.T("Atualizar vídeos e jogos")};UiTheme.Button(refresh,L.T("Atualizar"),"refresh");refresh.Margin=new Padding(0,25,0,0);refresh.Click+=(_,_)=>{if(!settings.PreferDesktop)GameDiscovery.Find(settings.GameExecutable,true);failedTargets.Clear();automatic.Reset();Discover();};toolbar.Controls.Add(refresh);layout.Controls.Add(toolbar,0,1);
        quality.Items.AddRange(new object[]{L.T("Até 1080p"),L.T("Até 720p"),L.T("Até 480p"),L.T("Adaptar ao tamanho")});quality.SelectedIndex=settings.Validated().Resolution switch{"720p"=>1,"480p"=>2,"auto"=>3,_=>0};
        frameRate.Items.AddRange(new object[]{"60 fps","30 fps","15 fps"});frameRate.SelectedIndex=settings.FramesPerSecond==15?2:settings.FramesPerSecond==30?1:0;
        var options=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};options.Controls.Add(UiTheme.Field(L.T("MONITOR"),monitors,230));options.Controls.Add(UiTheme.Field(L.T("RESOLUÇÃO"),quality,205));options.Controls.Add(UiTheme.Field("FPS",frameRate,110));dimensions.Margin=new Padding(10,31,0,0);dimensions.ForeColor=UiTheme.Muted;options.Controls.Add(dimensions);layout.Controls.Add(options,0,2);
        var editorTools=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};profileLabel.Margin=new Padding(0,8,12,0);editorTools.Controls.Add(profileLabel);
        UiTheme.Button(centerLayout,L.T("Centralizar"),"pip");centerLayout.Width=140;centerLayout.Click+=(_,_)=>editor.Center();editorTools.Controls.Add(centerLayout);
        UiTheme.Button(undoLayout,L.T("Desfazer posição"),"refresh");undoLayout.Width=180;undoLayout.Enabled=false;undoLayout.Click+=(_,_)=>editor.Undo();editor.HistoryChanged+=()=>undoLayout.Enabled=editor.CanUndo;editorTools.Controls.Add(undoLayout);
        UiTheme.Button(pickVideo,L.T("Vídeos"),"pip");pickVideo.Visible=true;pickVideo.Click+=(_,_)=>PickVideo();editorTools.Controls.Add(pickVideo);
        layout.Controls.Add(editorTools,0,4);layout.Controls.Add(editor,0,5);
        var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,16,0,0)};UiTheme.Button(start,L.T("Retomar"),"play",true);UiTheme.Button(stop,L.T("Pausar"),"stop");status.AutoSize=false;status.Size=new Size(650,44);status.TextAlign=ContentAlignment.MiddleLeft;status.ForeColor=UiTheme.Muted;footer.Controls.Add(start);footer.Controls.Add(stop);footer.Controls.Add(status);footer.SizeChanged+=(_,_)=>ResizeStatus();layout.Controls.Add(footer,0,6);
                var extras=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true};
        adaptiveBox.Checked=settings.AdaptiveFps;adaptiveBox.Margin=new Padding(0,27,16,0);extras.Controls.Add(adaptiveBox);
        minimumFps.Items.AddRange(new object[]{"15 fps","30 fps"});minimumFps.SelectedIndex=settings.MinimumFps==30?1:0;UiTheme.Combo(minimumFps);extras.Controls.Add(UiTheme.Field(L.T("FPS MÍNIMO"),minimumFps,110));
        updateChecks.Checked=settings.CheckForUpdates;updateChecks.Margin=new Padding(0,27,16,0);extras.Controls.Add(updateChecks);
        UiTheme.Button(updates,L.T("Atualizações"),"refresh");updates.Width=155;updates.Margin=new Padding(0,18,10,0);extras.Controls.Add(updates);
        UiTheme.Button(diagnostic,L.T("Diagnóstico"),"pip");diagnostic.Width=155;diagnostic.Margin=new Padding(0,18,0,0);extras.Controls.Add(diagnostic);
        var profileOptions=new FlowLayoutPanel{Width=1000,Height=54,WrapContents=false};
        snapBox.Checked=settings.SnapEnabled;snapBox.Margin=new Padding(0,12,12,0);profileOptions.Controls.Add(snapBox);
        margin.Value=settings.Validated().SnapMargin;marginLabel.Text=L.F("Margem: {0} px",margin.Value);marginLabel.Margin=new Padding(0,12,0,0);profileOptions.Controls.Add(marginLabel);profileOptions.Controls.Add(margin);
        var resetProfile=new ModernButton();UiTheme.Button(resetProfile,L.T("Restaurar padrão"),"refresh");resetProfile.Width=180;profileOptions.Controls.Add(resetProfile);
        var copyProfile=new ModernButton();UiTheme.Button(copyProfile,L.T("Copiar para jogo…"),"pip");copyProfile.Width=190;profileOptions.Controls.Add(copyProfile);extras.Controls.Add(profileOptions);
        snapBox.CheckedChanged+=(_,_)=>{if(syncing)return;settings.SnapEnabled=snapBox.Checked;Apply();};
        margin.ValueChanged+=(_,_)=>{marginLabel.Text=L.F("Margem: {0} px",margin.Value);if(syncing)return;settings.SnapMargin=margin.Value;Apply();};
        resetProfile.Click+=(_,_)=>{LoadProfile(GameProfile.From(new Settings{Monitor=SelectedScreen.DeviceName}));Save();status.Text=L.T("Perfil restaurado para o padrão.");};
        copyProfile.Click+=(_,_)=>{using var picker=new OpenFileDialog{Filter=L.T("Jogo Windows")+" (*.exe)|*.exe",Title=L.T("Copiar este perfil para outro jogo")};if(picker.ShowDialog(this)!=DialogResult.OK)return;try{string key=ProfileStore.Key(picker.FileName);if(profiles.Load(key)!=null&&MessageBox.Show(this,L.T("Substituir o perfil já salvo para esse jogo?"),L.T("Copiar perfil"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;profiles.Save(key,settings);status.Text=L.T("Perfil copiado. Será aplicado quando esse jogo conectar.");}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){status.Text=L.T("Não foi possível salvar o perfil.");}};
        layout.Controls.Add(extras,0,3);extras.Visible=false;layout.RowStyles[3].Height=0;
        adaptiveBox.CheckedChanged+=(_,_)=>{if(syncing)return;settings=settings with{AdaptiveFps=adaptiveBox.Checked};Apply();};
        minimumFps.SelectedIndexChanged+=(_,_)=>{if(syncing)return;settings=settings with{MinimumFps=minimumFps.SelectedIndex==1?30:15};Apply();};
        updateChecks.CheckedChanged+=(_,_)=>{settings.CheckForUpdates=updateChecks.Checked;Save();};
        updates.Click+=(_,_)=>{Save();using var dialog=new UpdateDialog(Path.GetDirectoryName(settingsPath)!);dialog.ShowDialog(this);};
        diagnostic.Click+=(_,_)=>{using var picker=new SaveFileDialog{Filter=L.T("Diagnóstico")+" JSON (*.json)|*.json",FileName="PiP-diagnostics.json"};if(picker.ShowDialog(this)==DialogResult.OK)try{File.WriteAllText(picker.FileName,SupportReport.Create(settings,session,session==null?0:elapsed.Elapsed.TotalSeconds,!automatic.Enabled));status.Text=L.T("Diagnóstico salvo, sem imagens, URLs ou caminhos pessoais.");}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){status.Text=L.T("Não foi possível salvar o diagnóstico.");}};
        toolbar.Visible=options.Visible=false;layout.RowStyles[1].Height=0;layout.RowStyles[2].Height=0;
        var advanced=new ModernButton{AccessibleName=L.T("Mostrar ou ocultar ajustes avançados")};UiTheme.Button(advanced,L.T("Ajustes"),"pip");advanced.Margin=new Padding(0,8,0,0);header.ColumnCount=3;header.ColumnStyles.Add(new(SizeType.Absolute,160));header.Controls.Add(advanced,2,0);

        RebuildMonitors();RestoreMonitorLayout();
        UpdateCanvas();if(!designPreview){RefreshSources();RefreshTargets();}
        editor.LayoutChanged+=box=>{settings=settings with {X=box.X,Y=box.Y,Width=box.Width,Height=box.Height};Apply();};
        monitors.SelectedIndexChanged+=(_,_)=>{if(syncing)return;RememberMonitorLayout();settings.PreferredMonitor=SelectedScreen.DeviceName;RestoreMonitorLayout();editor.ClearHistory();Stop("");automatic.Reset();UpdateCanvas();if(!designPreviewMode)Discover();};
        sources.SelectedIndexChanged+=(_,_)=>{if(refreshing)return;editor.ClearHistory();Stop("");automatic.Reset();PreviewSelection();Discover();};
        quality.SelectedIndexChanged+=(_,_)=>{if(syncing)return;settings=settings with {Resolution=quality.SelectedIndex switch{1=>"720p",2=>"480p",3=>"auto",_=>"1080p"}};Apply();};
        frameRate.SelectedIndexChanged+=(_,_)=>{if(syncing)return;settings=settings with {FramesPerSecond=frameRate.SelectedIndex==2?15:frameRate.SelectedIndex==1?30:60};Apply();};
        start.Click+=(_,_)=>ResumeAutomatic();stop.Click+=(_,_)=>PauseAutomatic();
        pulse.Tick+=(_,_)=>Poll();save.Tick+=(_,_)=>{save.Stop();Save();};
        targets.SelectedIndexChanged+=(_,_)=>{if(refreshing)return;if(targets.SelectedItem is RenderTarget chosen)failedTargets.Remove(chosen.Pid);settings.PreferDesktop=(targets.SelectedItem as RenderTarget)?.Backend=="desktop";Save();Stop("");automatic.Reset();Discover();};
        FormClosing+=(_,e)=>{if(!exiting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();tray.ShowBalloonTip(2500,L.T("PiP Overlay continua ativo"),L.T("Use o ícone perto do relógio para ajustar, pausar ou sair."),ToolTipIcon.Info);return;}Stop("");Save();};
        PreviewSelection();UpdateActions();status.Text=L.T("Abra o PiP no Edge ou Chrome. A overlay inicia sozinha.");
        discovery.Tick+=(_,_)=>Discover();if(!designPreview)discovery.Start();
        tray.Icon=Icon;tray.Text=L.T("PiP Overlay · automático");tray.Visible=!designPreview;
        VisibleChanged+=(_,_)=>{if(!Visible)editor.DetachSource();else PreviewSelection();};
        var menu=new ContextMenuStrip();menu.Items.Add(L.T("Ajustar posição e tamanho"),null,(_,_)=>ShowEditor());menu.Items.Add(L.T("Retomar"),null,(_,_)=>ResumeAutomatic());menu.Items.Add(L.T("Pausar"),null,(_,_)=>PauseAutomatic());menu.Items.Add(L.T("Sair"),null,(_,_)=>{exiting=true;Close();});tray.ContextMenuStrip=menu;tray.DoubleClick+=(_,_)=>ShowEditor();
        var launchGame=new ToolStripMenuItem(L.T("Iniciar jogo configurado"));menu.Items.Insert(0,launchGame);
        launchGame.Click+=async (_,_)=>{try{if(!File.Exists(settings.GameExecutable)){ShowEditor();using var picker=new OpenFileDialog{Filter=L.T("Jogo Windows")+" (*.exe)|*.exe",Title=L.T("Escolha o executável do seu jogo")};if(picker.ShowDialog(this)!=DialogResult.OK)return;settings.GameExecutable=picker.FileName;Save();}launchGame.Enabled=false;status.Text=L.T("Preparando seu jogo…");await GameLauncher.Launch(settings.GameExecutable);automatic.Reset();Discover();}catch(Exception ex){ShowEditor();status.Text=ex.Message;}finally{launchGame.Enabled=true;}};
        var chooseGame=new ModernButton();UiTheme.Button(chooseGame,L.T("Trocar jogo"),"pip");chooseGame.Width=150;chooseGame.Margin=new Padding(12,25,0,0);options.Controls.Add(chooseGame);chooseGame.Click+=(_,_)=>{using var picker=new OpenFileDialog{Filter=L.T("Jogo Windows")+" (*.exe)|*.exe"};if(picker.ShowDialog(this)==DialogResult.OK){settings.GameExecutable=picker.FileName;Save();GameDiscovery.Find(settings.GameExecutable,true);automatic.Reset();Discover();status.Text=L.T("Jogo selecionado. A opção de iniciar fica no ícone perto do relógio.");}};
        var shortcuts=new ModernButton{AccessibleName=L.T("Configurar atalhos de teclado")};UiTheme.Button(shortcuts,L.T("Atalhos"),"keyboard");shortcuts.Margin=new Padding(12,25,0,0);toolbar.Controls.Add(shortcuts);shortcuts.Click+=(_,_)=>ConfigureShortcuts();
        ConfigureWorkspace(layout,header,footer,advanced,refresh,chooseGame,shortcuts,resetProfile,copyProfile);
        if(!designPreview)Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;
        Shown+=async (_,_)=>{if(designPreview)return;hotkeys=new Hotkeys(Handle);windowEvents=new WindowEvents(()=>{if(!settings.PreferDesktop)GameDiscovery.Invalidate();Discover();});Discover();if(!hotkeys.Apply(new(settings.StartShortcut),new(settings.StopShortcut),out var error))status.Text=error;if(settings.CheckForUpdates)await CheckUpdates(false);};
        if(designPreview){save.Stop();exiting=true;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(-20000,-20000);}
    }
    void SelectProfile(string key,string name){
        if(profileKey==key)return;
        if(profileKey!=null)try{profiles.Save(profileKey,settings);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){status.Text=L.T("Não foi possível salvar o perfil anterior.");}
        profileKey=key;profileName=name;profileLabel.Text=L.F("Perfil: {0}",name);editor.ClearHistory();var profile=profiles.Load(key);if(profile!=null)LoadProfile(profile);
    }
    void LoadProfile(GameProfile profile){
        syncing=true;
        try{settings=profile.Apply(settings);snapBox.Checked=settings.SnapEnabled;margin.Value=settings.SnapMargin;editor.ClearHistory();quality.SelectedIndex=settings.Resolution switch{"720p"=>1,"480p"=>2,"auto"=>3,_=>0};frameRate.SelectedIndex=settings.FramesPerSecond==15?2:settings.FramesPerSecond==30?1:0;adaptiveBox.Checked=settings.AdaptiveFps;minimumFps.SelectedIndex=settings.MinimumFps==30?1:0;
            RebuildMonitors();RestoreMonitorLayout();
            UpdateCanvas();
        }finally{syncing=false;}
    }
    WindowSource[] AvailableVideos()=>WindowSource.Find().Where(x=>x.LikelyPip).Concat(active is {Exists:true}?new[]{active}:Array.Empty<WindowSource>()).DistinctBy(x=>(x.Handle,x.ProcessId)).ToArray();
    void RefreshVideoChoices(){
        lastVideoRefresh=Environment.TickCount64;var found=AvailableVideos();var old=sources.SelectedItem as WindowSource;
        refreshing=true;try{sources.Items.Clear();sources.Items.AddRange(found);sources.SelectedItem=found.FirstOrDefault(x=>x.Handle==old?.Handle&&x.ProcessId==old?.ProcessId)??found.FirstOrDefault();pickVideo.Visible=true;}finally{refreshing=false;}
    }
    void PickVideo(){
        pickingVideo=true;
        try{RefreshVideoChoices();var found=sources.Items.Cast<WindowSource>().ToArray();if(found.Length==0){status.Text=ConnectionMessages.WaitingVideo;return;}
            using var picker=new VideoPicker(found);if(picker.ShowDialog(this)==DialogResult.OK&&picker.Selected is WindowSource selected)sources.SelectedItem=selected;
        }finally{pickingVideo=false;Discover();}
    }
    async Task CheckUpdates(bool manual){
        if(checkingUpdate)return;
        string stamp=Path.Combine(AppContext.BaseDirectory,"data","update-check.txt");
        if(!manual&&File.Exists(stamp)&&DateTime.UtcNow-File.GetLastWriteTimeUtc(stamp)<TimeSpan.FromDays(1))return;
        checkingUpdate=true;updates.Enabled=false;
        try{availableRelease=await UpdateService.Check();if(IsDisposed)return;
            Directory.CreateDirectory(Path.GetDirectoryName(stamp)!);File.WriteAllText(stamp,DateTime.UtcNow.ToString("O"));
            if(availableRelease!=null){updates.Text=L.F("Baixar {0}",availableRelease.Version);tray.ShowBalloonTip(4000,L.T("Nova versão do PiP Overlay"),L.F("Versão {0} disponível em Ajustes > Atualizações.",availableRelease.Version),ToolTipIcon.Info);}
            else if(manual)status.Text=L.T("Você está usando a versão mais recente.");
        }catch(Exception ex)when(ex is System.Net.Http.HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException){if(manual&&!IsDisposed)status.Text=L.T("Não foi possível verificar atualizações agora.");}
        finally{checkingUpdate=false;if(!IsDisposed)updates.Enabled=true;}
    }
    void ConfigureShortcuts(){if(hotkeys==null)return;configuringShortcuts=true;try{using var dialog=new ShortcutDialog(new(settings.StartShortcut),new(settings.StopShortcut),(a,b)=>{if(!hotkeys.Apply(a,b,out var error))return error;settings=settings with{StartShortcut=a.Value,StopShortcut=b.Value};Save();return null;});shortcutDialog=dialog;dialog.ShowDialog(this);}finally{shortcutDialog=null;configuringShortcuts=false;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x312&&hotkeys!=null){if(configuringShortcuts)shortcutDialog?.ReceiveRegisteredShortcut(hotkeys.ShortcutFor((int)m.WParam));else{int action=hotkeys.ActionFor((int)m.WParam);if(action==0)ResumeAutomatic();else if(action==1)PauseAutomatic();}return;}base.WndProc(ref m);}
    void ShowEditor(){Show();WindowState=FormWindowState.Normal;Activate();}
    void UpdateActions(){start.Visible=!automatic.Enabled||connectionFailed;start.Text=connectionFailed?L.T("Tentar novamente"):L.T("Retomar");start.Enabled=true;stop.Visible=automatic.Enabled;stop.Enabled=true;ResizeStatus();}
    void ResizeStatus(){if(status.Parent is Control parent)status.Width=Math.Max(120,parent.ClientSize.Width-parent.Padding.Horizontal-(start.Visible?start.Width+start.Margin.Horizontal:0)-(stop.Visible?stop.Width+stop.Margin.Horizontal:0)-status.Margin.Horizontal);}
    void PauseAutomatic(){automatic.Pause();connectionFailed=false;settings.AutomaticEnabled=false;Stop(L.T("Pausado · clique em Retomar quando quiser."));PreviewSelection();Save();}
    void ResumeAutomatic(){automatic.Resume();failedTargets.Clear();connectionFailed=false;settings.AutomaticEnabled=true;Save();Discover();}
    void Fail(string message){
        var target=targets.SelectedItem as RenderTarget;connectionFailed=true;Stop(message);
        if(automatic.Enabled&&!settings.PreferDesktop&&target is {Backend:not "desktop"}&&failedTargets.Add(target.Pid)){
            var independent=targets.Items.Cast<RenderTarget>().FirstOrDefault(x=>x.Backend=="desktop");
            if(independent!=null){refreshing=true;try{targets.SelectedItem=independent;}finally{refreshing=false;}automatic.Reset();if(IsHandleCreated)BeginInvoke(()=>{if(!IsDisposed&&automatic.Enabled&&session==null)Discover();});}
        }
    }
    void Discover(){
        discovery.Interval=!Visible?15000:5000;
        if(!automatic.Enabled&&!Visible)return;
        if(configuringShortcuts||refreshing||pickingVideo)return;
        if(session!=null){if(activeTarget is int pid){try{using var p=System.Diagnostics.Process.GetProcessById(pid);if(!p.HasExited){if(Visible&&Environment.TickCount64-lastVideoRefresh>5000){RefreshVideoChoices();RefreshGameChoices();}return;}}catch(ArgumentException){}}Stop(L.T("Aguardando o jogo abrir…"));}
        refreshing=true;
        try{
            var found=WindowSource.Find().Where(x=>x.LikelyPip).ToArray();
            pickVideo.Visible=true;
            var games=AvailableTargets();
            if(!found.Select(x=>(x.Handle,x.ProcessId)).SequenceEqual(sources.Items.Cast<WindowSource>().Select(x=>(x.Handle,x.ProcessId)))){var old=sources.SelectedItem as WindowSource;sources.Items.Clear();sources.Items.AddRange(found);sources.SelectedItem=found.FirstOrDefault(x=>x.Handle==old?.Handle&&x.ProcessId==old?.ProcessId)??found.FirstOrDefault();PreviewSelection();}
            if(!games.SequenceEqual(targets.Items.Cast<RenderTarget>())){var old=targets.SelectedItem as RenderTarget;targets.Items.Clear();targets.Items.AddRange(games);targets.SelectedItem=games.FirstOrDefault(x=>x.Pid==old?.Pid)??games.FirstOrDefault();}
            var source=sources.SelectedItem as WindowSource;var target=targets.SelectedItem as RenderTarget;
            UpdateActions();tray.Text=automatic.Enabled?L.T("PiP Overlay · automático"):L.T("PiP Overlay · pausado");
            if(target?.UnavailableReason is string reason&&automatic.Enabled)status.Text=reason;
            else if(automatic.TakeAttempt(source==null?null:$"{source.Handle}:{source.ProcessId}",target?.Pid,false))Start();
            else if(!automatic.Enabled)status.Text=L.T("Pausado · clique em Retomar quando quiser.");
            else if(source==null)status.Text=ConnectionMessages.WaitingVideo;
            else if(target==null)status.Text=ConnectionMessages.WaitingGame;
        }finally{refreshing=false;}
    }
    protected override void OnHandleDestroyed(EventArgs e){windowEvents?.Dispose();windowEvents=null;hotkeys?.Dispose();hotkeys=null;base.OnHandleDestroyed(e);}
    Screen SelectedScreen=>((MonitorItem?)monitors.SelectedItem)?.Screen??Screen.AllScreens[0];
    void UpdateCanvas(){editor.MonitorSize=SelectedScreen.Bounds.Size;settings=settings with {Monitor=SelectedScreen.DeviceName};var b=LayoutMath.Constrain(new(settings.X,settings.Y,settings.Width,settings.Height),editor.MonitorSize,editor.Aspect);settings=settings with{X=b.X,Y=b.Y,Width=b.Width,Height=b.Height};editor.Video=b;Apply();}
    void Apply(){editor.SnapEnabled=settings.SnapEnabled;editor.SnapMargin=settings.SnapMargin;try{lease?.EnsureResolution(settings.SourceResolution);}catch(System.ComponentModel.Win32Exception ex){Fail(ex.Message);}editor.Video=new(settings.X,settings.Y,settings.Width,settings.Height);editor.Invalidate();session?.Apply(settings);desktopOverlay?.Apply(settings,SelectedScreen.Bounds);
        dimensions.Text=L.F("Vídeo · {0:F0}% da largura da tela",100.0*settings.Width/editor.MonitorSize.Width);
        save.Stop();save.Start();}
    void Save(){if(designPreviewMode||syncing||preferencesImported)return;try{RememberMonitorLayout();settings.Save(settingsPath);if(profileKey!=null)profiles.Save(profileKey,settings);}catch(Exception e)when(e is IOException or UnauthorizedAccessException){status.Text=L.T("Não foi possível salvar: ")+e.Message;}}
    void RefreshSources(){Stop("");sources.Items.Clear();foreach(var source in WindowSource.Find().Where(x=>x.LikelyPip))sources.Items.Add(source);if(sources.Items.Count>0)sources.SelectedIndex=0;else status.Text=L.T("Abra o PiP do navegador. Vídeo e jogo são detectados automaticamente.");}
    void PreviewSelection(){if(sources.SelectedItem is WindowSource source&&source.Exists){Native.GetWindowRect(source.Handle,out var rect);if(rect.Right>rect.Left&&rect.Bottom>rect.Top){editor.Aspect=(double)(rect.Right-rect.Left)/(rect.Bottom-rect.Top);UpdateCanvas();}editor.AttachSource(source.Handle);}}
    RenderTarget[] AvailableTargets()=>(settings.PreferDesktop?Enumerable.Empty<RenderTarget>():GameLauncher.Targets().Concat(GameDiscovery.Find(settings.GameExecutable)))
        .Append(new RenderTarget(Environment.ProcessId,L.T("Área de trabalho · janela/borderless"),false,true,Backend:"desktop"))
        .DistinctBy(x=>x.Pid).OrderBy(x=>x.UnavailableReason!=null).ThenBy(x=>settings.PreferDesktop?x.Backend!="desktop":x.Backend=="desktop").ToArray();
    void RefreshGameChoices(){
        var games=AvailableTargets();
        if(games.SequenceEqual(targets.Items.Cast<RenderTarget>()))return;
        var selected=targets.SelectedItem as RenderTarget;
        refreshing=true;try{targets.Items.Clear();targets.Items.AddRange(games);targets.SelectedItem=games.FirstOrDefault(x=>x.Pid==selected?.Pid)??games.FirstOrDefault();}finally{refreshing=false;}
    }
    void RefreshTargets(){Stop("");targets.Items.Clear();targets.Items.AddRange(AvailableTargets());if(targets.Items.Count>0)targets.SelectedIndex=0;}
    void Start(){if(sources.SelectedItem is not WindowSource source){status.Text=ConnectionMessages.WaitingVideo;return;}connectionFailed=false;Stop("");try{
        if(!source.Exists||Native.IsIconic(source.Handle))throw new InvalidOperationException(L.T("Restaure o PiP antes de iniciar."));
        if(targets.SelectedItem is not RenderTarget target)throw new InvalidOperationException(L.T("Abra seu jogo. Para novos renderizadores, use o menu do ícone perto do relógio."));
        if(target.UnavailableReason is string reason)throw new InvalidOperationException(reason);
        if(target.Backend!="desktop")try{using var process=System.Diagnostics.Process.GetProcessById(target.Pid);if(process.MainModule?.FileName is string file)SelectProfile(ProfileStore.Key(file),Path.GetFileNameWithoutExtension(file));}catch(System.ComponentModel.Win32Exception){}
        lease=new SourceLease(source,settings.SourceResolution);
        if(target.Backend=="desktop"){desktopOverlay=new DesktopOverlay(settings,SelectedScreen.Bounds);desktopOverlay.ShowOverlay();}
        session=new GpuVideoSession(source,settings,editor.MonitorSize,target);editor.Aspect=(double)session.SourceSize.Width/session.SourceSize.Height;UpdateCanvas();editor.AttachSource(source.Handle);
        active=source;activeTarget=target.Pid;start.Enabled=false;stop.Enabled=true;elapsed.Restart();lastFrames=0;lastTick=Environment.TickCount64;pulse.Start();status.Text=desktopOverlay!=null?L.T("Conectando à área de trabalho…"):ConnectionMessages.Connecting;
    }catch(Exception e){Fail(e.Message);}}
    void Stop(string message){pulse.Stop();session?.Dispose();session=null;desktopOverlay?.Dispose();desktopOverlay=null;editor.DetachSource();lease?.Dispose();lease=null;active=null;activeTarget=null;UpdateActions();status.Text=message;}
    void Poll(){if(session==null||active==null)return;
        if(!active.Exists){Stop(ConnectionMessages.Interrupted);automatic.Reset();return;}
        if(session.Error is string error){Fail(error);return;}
        if(session.Published==0&&elapsed.Elapsed.TotalSeconds>8){Fail(ConnectionMessages.Interrupted);automatic.RetryTransient();return;}
        if(!session.Ready&&elapsed.Elapsed.TotalSeconds>20){Fail(ConnectionMessages.Unavailable);return;}
        if(Visible&&WindowState!=FormWindowState.Minimized)editor.RefreshThumbnail();lease?.KeepBehind();
        long now=Environment.TickCount64;if(now-lastTick>=1000){long count=session.Published;double fps=(count-lastFrames)*1000.0/(now-lastTick);lastFrames=count;lastTick=now;status.Text=session.Rendered>0?L.F(desktopOverlay!=null?"Desktop ativo · {0}×{1} · {2:F0} fps":"Renderizador conectado · {0}×{1} · {2:F0} fps",session.TransferSize.Width,session.TransferSize.Height,fps)+(settings.AdaptiveFps?L.F(" · automático até {0} fps",session.EffectiveFps):""):desktopOverlay!=null?L.T("Aguardando o primeiro quadro…"):L.T("Aguardando o jogo ficar visível…");}
    }
    protected override void Dispose(bool disposing){if(disposing){Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;windowEvents?.Dispose();windowEvents=null;tray.Visible=false;tray.ContextMenuStrip?.Dispose();tray.Dispose();discovery.Dispose();pulse.Dispose();save.Dispose();session?.Dispose();session=null;desktopOverlay?.Dispose();desktopOverlay=null;lease?.Dispose();lease=null;}base.Dispose(disposing);}
    sealed record MonitorItem(Screen Screen){public override string ToString()=>$"{Screen.DeviceName.Replace("\\\\.\\","")} · {Screen.Bounds.Width}×{Screen.Bounds.Height}";}
}
