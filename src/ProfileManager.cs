namespace PipOverlay;

internal static class ProfileMapping
{
    internal static GameProfile Remap(GameProfile profile,string from,string to,Size screen){
        var settings=profile.Apply(new Settings());
        if(!settings.MonitorLayouts.TryGetValue(from,out var layout))throw new InvalidDataException("Unknown saved monitor");
        settings=layout.Apply(settings,screen) with{Monitor=to,PreferredMonitor=to};
        settings.MonitorLayouts[to]=DisplayLayout.Capture(settings,screen);
        return GameProfile.From(settings) with{Name=profile.Name};
    }
}
internal sealed class ProfileManager:Form
{
    sealed record Entry(string Key,GameProfile Profile){public override string ToString()=>string.IsNullOrWhiteSpace(Profile.Name)?L.F("Perfil {0}",Key[..Math.Min(8,Key.Length)]):Profile.Name;}
    sealed record Display(Screen Screen){public override string ToString()=>$"{Screen.DeviceName} · {Screen.Bounds.Width}×{Screen.Bounds.Height}";}
    readonly ModernComboBox profiles=new(){DropDownStyle=ComboBoxStyle.DropDownList},savedMonitor=new(){DropDownStyle=ComboBoxStyle.DropDownList},localMonitor=new(){DropDownStyle=ComboBoxStyle.DropDownList};
    readonly Label message=new(){AutoSize=true,MaximumSize=new(560,0),ForeColor=UiTheme.Muted};
    readonly ProfileStore store;readonly Action<GameProfile> apply;
    internal ProfileManager(string folder,Settings current,Action<GameProfile> applyProfile){
        store=new(folder);apply=applyProfile;Text=L.T("Perfis e monitores");Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(15,20,30);ForeColor=Color.White;ClientSize=new(640,530);MinimumSize=new(600,500);StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;
        var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};Controls.Add(flow);
        void Field(string name,Control control){UiTheme.Combo((ComboBox)control);control.AccessibleName=name;var field=UiTheme.Field(name,control,560);field.Margin=new(0,0,0,12);flow.Controls.Add(field);}
        void Button(string name,Action action){var button=new ModernButton();UiTheme.Button(button,name,"pip");button.Width=560;button.Margin=new(0,0,0,10);button.Click+=(_,_)=>{try{action();}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){message.Text=L.T("Não foi possível salvar o perfil.");}};flow.Controls.Add(button);}
        Field(L.T("Perfil salvo"),profiles);
        profiles.Items.Add(new Entry("current",GameProfile.From(current) with{Name=L.T("Perfil atual")}));
        if(Directory.Exists(folder))foreach(string file in Directory.EnumerateFiles(folder,"*.json")){
            string key=Path.GetFileNameWithoutExtension(file);if(System.Text.RegularExpressions.Regex.IsMatch(key,"^[A-F0-9]{64}$")&&store.Load(key) is {} profile)profiles.Items.Add(new Entry(key,profile));
        }
        Button(L.T("Aplicar perfil"),()=>{if(profiles.SelectedItem is Entry entry){apply(entry.Profile);message.Text=L.T("Perfil aplicado.");}});
        Button(L.T("Associar a um jogo…"),()=>{
            if(profiles.SelectedItem is not Entry entry)return;using var picker=new OpenFileDialog{Filter=L.T("Jogo Windows")+" (*.exe)|*.exe",Title=L.T("Escolha o executável do seu jogo")};
            if(picker.ShowDialog(this)!=DialogResult.OK)return;string key=ProfileStore.Key(picker.FileName);
            if(store.Load(key)!=null&&MessageBox.Show(this,L.T("Substituir o perfil já salvo para esse jogo?"),Text,MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            store.Save(key,entry.Profile.Apply(new Settings()),Path.GetFileNameWithoutExtension(picker.FileName));message.Text=L.T("Perfil copiado. Será aplicado quando esse jogo conectar.");
        });
        Field(L.T("Monitor salvo"),savedMonitor);Field(L.T("Monitor neste computador"),localMonitor);
        foreach(var screen in Screen.AllScreens)localMonitor.Items.Add(new Display(screen));localMonitor.SelectedIndex=0;
        Button(L.T("Associar monitor e aplicar"),()=>{
            if(profiles.SelectedItem is not Entry entry||savedMonitor.SelectedItem is not string old||localMonitor.SelectedItem is not Display display)return;
            var profile=ProfileMapping.Remap(entry.Profile,old,display.Screen.DeviceName,display.Screen.Bounds.Size);
            if(entry.Key!="current")store.Save(entry.Key,profile.Apply(new Settings()),profile.Name);
            apply(profile);profiles.Items[profiles.SelectedIndex]=entry with{Profile=profile};message.Text=L.T("Perfil aplicado.");
        });
        profiles.SelectedIndexChanged+=(_,_)=>{savedMonitor.Items.Clear();if(profiles.SelectedItem is Entry entry)savedMonitor.Items.AddRange(entry.Profile.Apply(new Settings()).MonitorLayouts.Keys.Cast<object>().ToArray());if(savedMonitor.Items.Count>0)savedMonitor.SelectedIndex=0;};profiles.SelectedIndex=0;
        message.Text=L.T("Escolha um perfil importado e associe ao executável ou monitor deste computador. O perfil original é preservado ao associar outro jogo.");flow.Controls.Add(message);
        flow.SizeChanged+=(_,_)=>{foreach(Control control in flow.Controls)control.Width=Math.Max(320,flow.ClientSize.Width-flow.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-4);};
    }
}
