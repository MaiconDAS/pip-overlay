namespace PipOverlay;
internal sealed partial class MainForm
{
    void ConfigureWorkspace(TableLayoutPanel old,Control header,Control footer,Button toggle,Button refresh,Button chooseGame,Button shortcuts,Button reset,Button copy){
        SuspendLayout();
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=3};
        root.RowStyles.Add(new(SizeType.Absolute,88));root.RowStyles.Add(new(SizeType.Percent,100));root.RowStyles.Add(new(SizeType.Absolute,70));
        root.Controls.Add(header,0,0);root.Controls.Add(footer,0,2);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
        body.ColumnStyles.Add(new(SizeType.Percent,100));body.ColumnStyles.Add(new(SizeType.Absolute,0));root.Controls.Add(body,0,1);
        var canvas=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty};
        canvas.RowStyles.Add(new(SizeType.Absolute,42));canvas.RowStyles.Add(new(SizeType.Percent,100));canvas.RowStyles.Add(new(SizeType.Absolute,44));body.Controls.Add(canvas,0,0);
        var tools=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=Padding.Empty};
        tools.ColumnStyles.Add(new(SizeType.Percent,100));foreach(int width in new[]{112,110,104})tools.ColumnStyles.Add(new(SizeType.Absolute,width));
        profileLabel.Dock=DockStyle.Fill;profileLabel.Margin=Padding.Empty;var identity=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};identity.RowStyles.Add(new(SizeType.Percent,50));identity.RowStyles.Add(new(SizeType.Percent,50));identity.Controls.Add(modeLabel);identity.Controls.Add(profileLabel);tools.Controls.Add(identity,0,0);
        Compact(centerLayout,L.T("Centralizar"));Compact(undoLayout,L.T("Desfazer"));Compact(pickVideo,L.T("Vídeos"));
        tools.Controls.Add(centerLayout,1,0);tools.Controls.Add(undoLayout,2,0);tools.Controls.Add(pickVideo,3,0);
        canvas.Controls.Add(tools,0,0);canvas.Controls.Add(editor,0,1);
        canvas.Controls.Add(new Label{Text=L.T("Arraste ou use as setas · Ctrl+setas redimensiona · Shift acelera · Ctrl+Z desfaz"),Dock=DockStyle.Fill,ForeColor=UiTheme.Muted,Font=new Font(Font.FontFamily,9),TextAlign=ContentAlignment.MiddleLeft},0,2);
        var tabs=new Panel{Dock=DockStyle.Fill,BackColor=UiTheme.Panel,Padding=new Padding(1),Margin=new Padding(14,0,0,0)};
        var navigation=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false,Padding=new Padding(5,2,0,0)};
        var pages=new Panel{Dock=DockStyle.Fill,BackColor=BackColor,Padding=new Padding(12)};tabs.Controls.Add(pages);tabs.Controls.Add(navigation);
        var sections=new List<(FlowLayoutPanel Page,Button Tab)>();
        FlowLayoutPanel Page(string name){
            var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Visible=sections.Count==0};pages.Controls.Add(flow);
            bool sizing=false;
            flow.Layout+=(_,_)=>{if(sizing)return;sizing=true;try{int width=Math.Max(160,flow.Width-SystemInformation.VerticalScrollBarWidth-4);foreach(Control child in flow.Controls){int desired=Math.Max(120,width-child.Margin.Horizontal);if(child.Width!=desired)child.Width=desired;}}finally{sizing=false;}};
            var tab=new ModernButton{Text=name,Width=88,Height=32,FlatStyle=FlatStyle.Flat,BackColor=UiTheme.Panel,ForeColor=sections.Count==0?UiTheme.Accent:UiTheme.Muted,Margin=Padding.Empty,Cursor=Cursors.Hand};tab.FlatAppearance.BorderSize=0;navigation.Controls.Add(tab);sections.Add((flow,tab));
            tab.Click+=(_,_)=>{foreach(var section in sections){section.Page.Visible=section.Page==flow;section.Tab.ForeColor=section.Page==flow?UiTheme.Accent:UiTheme.Muted;}flow.BringToFront();};return flow;
        }
        void Field(FlowLayoutPanel page,string label,Control control){control.Visible=true;control.Margin=Padding.Empty;control.AccessibleName=label;var field=UiTheme.Field(label,control,240);field.Margin=new Padding(0,0,0,8);page.Controls.Add(field);}
        void Item(FlowLayoutPanel page,Control control){control.Dock=DockStyle.None;control.Visible=true;if(control is ModernToggle)control.AutoSize=false;control.Margin=new Padding(0,4,0,10);if(control is Button button){Compact(button,button.Text);button.Width=258;}page.Controls.Add(control);}
        var picture=Page(L.T("Imagem"));Field(picture,L.T("RESOLUÇÃO · NITIDEZ"),quality);Field(picture,L.T("FPS · FLUIDEZ"),frameRate);
        Item(picture,new Label{Text=L.T("Resolução e FPS são independentes.\n30 fps mantém a nitidez escolhida."),Width=258,Height=48,ForeColor=UiTheme.Muted});Item(picture,adaptiveBox);Field(picture,L.T("MÍNIMO NO MODO AUTOMÁTICO"),minimumFps);
        minimumFps.Enabled=adaptiveBox.Checked;adaptiveBox.CheckedChanged+=(_,_)=>minimumFps.Enabled=adaptiveBox.Checked;
        var position=Page(L.T("Posição"));Field(position,L.T("MONITOR"),monitors);Item(position,dimensions);dimensions.AutoSize=false;dimensions.Size=new Size(258,35);Item(position,snapBox);Item(position,marginLabel);margin.Width=258;margin.TickStyle=TickStyle.None;Item(position,margin);Item(position,reset);Item(position,copy);
        var more=Page(L.T("Mais"));
        var language=new ModernComboBox{DropDownStyle=ComboBoxStyle.DropDownList};UiTheme.Combo(language);
        language.Items.AddRange(L.Languages);language.SelectedItem=L.Languages.First(x=>x.Code==settings.Language);
        Field(more,L.T("IDIOMA"),language);
        language.SelectedIndexChanged+=(_,_)=>{
            if(language.SelectedItem is not L.Language selected||selected.Code==settings.Language)return;
            string previous=L.Current;settings.Language=selected.Code;L.Set(selected.Code);
            bool wasSyncing=syncing;syncing=true;
            try{L.Refresh(this,previous);if(tray.ContextMenuStrip!=null)L.RefreshMenu(tray.ContextMenuStrip.Items,previous);}
            finally{syncing=wasSyncing;}
            marginLabel.Text=L.F("Margem: {0} px",margin.Value);profileLabel.Text=profileName.Length==0?L.T("Perfil: Geral"):L.F("Perfil: {0}",profileName);UpdateActions();Apply();Save();
        };
        var direct=new ModernToggle{Text=L.T("Conectar diretamente ao jogo"),Checked=!settings.PreferDesktop,Height=48};Item(more,direct);
        var explanation=new Label{Text=L.T("Modo desktop: funciona sobre janelas e jogos borderless, sem selecionar um processo."),Height=76,ForeColor=UiTheme.Muted};Item(more,explanation);
        Field(more,L.T("VÍDEO DO NAVEGADOR"),sources);Field(more,L.T("CONEXÃO AVANÇADA"),targets);Item(more,refresh);Item(more,chooseGame);
        void ConnectionControls(){targets.Parent!.Visible=direct.Checked;chooseGame.Visible=direct.Checked;refresh.Visible=direct.Checked;explanation.Visible=!direct.Checked;}
        targets.SelectedIndexChanged+=(_,_)=>{if(refreshing)return;bool previous=syncing;syncing=true;try{direct.Checked=!settings.PreferDesktop;ConnectionControls();}finally{syncing=previous;}};
        ConnectionControls();direct.CheckedChanged+=(_,_)=>{if(syncing)return;settings.PreferDesktop=!direct.Checked;settings.OutputModeVersion=1;Stop("");refreshing=true;try{targets.Items.Clear();targets.Items.AddRange(AvailableTargets());if(targets.Items.Count>0)targets.SelectedIndex=0;}finally{refreshing=false;}automatic.Reset();ConnectionControls();Save();if(!designPreviewMode)Discover();};
        Item(more,shortcuts);
        var manageProfiles=new ModernButton();UiTheme.Button(manageProfiles,L.T("Perfis e monitores"),"pip");manageProfiles.Click+=(_,_)=>{Save();using var dialog=new ProfileManager(Path.Combine(AppContext.BaseDirectory,"data","profiles"),settings,p=>{Stop("");LoadProfile(p);Save();automatic.Reset();Discover();});dialog.ShowDialog(this);};Item(more,manageProfiles);
        foreach(var action in new[]{(L.T("Exportar preferências"),(Action)ExportPreferences),(L.T("Importar preferências"),(Action)ImportPreferences)}){var button=new ModernButton{Text=action.Item1};UiTheme.Button(button,action.Item1,"pip");button.Click+=(_,_)=>action.Item2();Item(more,button);}
        Item(more,updateChecks);Item(more,updates);Item(more,diagnostic);
        tabs.Visible=false;body.Controls.Add(tabs,1,0);
        toggle.Click+=(_,_)=>{bool open=!tabs.Visible;body.ColumnStyles[1].Width=open?316:0;tabs.Visible=open;toggle.Text=open?L.T("Fechar"):L.T("Ajustes");};
        Controls.Remove(old);Controls.Add(root);old.Dispose();ResumeLayout(true);
    }
    static void Compact(Button button,string text){button.Text=text;button.Image?.Dispose();button.Image=null;button.Height=34;button.Width=104;button.Padding=new Padding(6,0,4,0);button.Margin=new Padding(0,3,6,3);button.Font=new Font("Segoe UI",9);}
}
