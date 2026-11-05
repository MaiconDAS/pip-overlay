namespace PipOverlay;

internal sealed partial class MainForm
{
    void RememberMonitorLayout(){
        if(string.IsNullOrEmpty(settings.Monitor))return;
        settings.MonitorLayouts[settings.Monitor]=DisplayLayout.Capture(settings,editor.MonitorSize);
    }
    void RebuildMonitors(){
        bool previous=syncing;syncing=true;
        try{
            if(string.IsNullOrEmpty(settings.PreferredMonitor))settings.PreferredMonitor=settings.Monitor;
            monitors.Items.Clear();foreach(var screen in Screen.AllScreens)monitors.Items.Add(new MonitorItem(screen));
            monitors.SelectedItem=monitors.Items.Cast<MonitorItem>().FirstOrDefault(x=>x.Screen.DeviceName==settings.PreferredMonitor)
                ??monitors.Items.Cast<MonitorItem>().First(x=>x.Screen.Primary);
        }finally{syncing=previous;}
    }
    void RestoreMonitorLayout(){
        if(settings.MonitorLayouts.TryGetValue(SelectedScreen.DeviceName,out var layout))settings=layout.Apply(settings,SelectedScreen.Bounds.Size);
        else if(settings.MonitorLayouts.TryGetValue(settings.Monitor,out var old))settings=old.Apply(settings,SelectedScreen.Bounds.Size);
    }
    void DisplayChanged(object? sender,EventArgs e){
        if(IsDisposed||!IsHandleCreated)return;
        try{BeginInvoke(()=>{
            if(IsDisposed)return;
            RememberMonitorLayout();Stop("");RebuildMonitors();RestoreMonitorLayout();editor.ClearHistory();UpdateCanvas();Save();automatic.Reset();Discover();
        });}catch(InvalidOperationException){}
    }
    void ExportPreferences(){
        using var picker=new SaveFileDialog{Filter="JSON (*.json)|*.json",FileName="PiP-preferences.json",Title=L.T("Exportar preferências")};
        if(picker.ShowDialog(this)!=DialogResult.OK)return;
        try{Save();PreferencesArchive.Export(Path.GetDirectoryName(settingsPath)!,picker.FileName);status.Text=L.T("Preferências exportadas.");}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException){status.Text=L.T("Não foi possível salvar o perfil.");}
    }
    void ImportPreferences(){
        using var picker=new OpenFileDialog{Filter="JSON (*.json)|*.json",Title=L.T("Importar preferências")};
        if(picker.ShowDialog(this)!=DialogResult.OK)return;
        try{
            PreferencesArchive.Read(picker.FileName);
            if(MessageBox.Show(this,L.T("Importar ajustes e perfis? Uma cópia dos ajustes atuais será salva. O PiP será pausado; reinicie o aplicativo para aplicar."),L.T("Importar preferências"),MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
            Save();PreferencesArchive.Import(Path.GetDirectoryName(settingsPath)!,picker.FileName);
            // Prevent shutdown/save timers from replacing the imported file.
            preferencesImported=true;save.Stop();settings=Settings.Load(settingsPath);profileKey=null;automatic.Pause();Stop("");
            exiting=true;Close();
        }catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException){status.Text=L.T("Arquivo de preferências inválido.");}
    }
}
