namespace PipOverlay;
internal sealed class UpdateDialog:Form
{
    readonly CancellationTokenSource cancellation=new();
    readonly ModernComboBox versions=new(){Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly Label message=new(){Dock=DockStyle.Fill,Padding=new Padding(0,12,0,0)};
    readonly ModernButton download=new(){Text=L.T("Baixar e verificar"),Enabled=false};
    readonly ModernButton folder=new(){Text=L.T("Abrir pasta"),Visible=false};
    readonly string root;bool busy;
    internal UpdateDialog(string dataRoot){
        root=dataRoot;Text=L.T("Atualizações");Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(15,20,30);ForeColor=Color.White;ClientSize=new Size(640,380);MinimumSize=new Size(600,380);StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=4};layout.RowStyles.Add(new(SizeType.Absolute,42));layout.RowStyles.Add(new(SizeType.Absolute,42));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,50));Controls.Add(layout);
        layout.Controls.Add(new Label{Text=L.F("Versão instalada: {0}",UpdateService.Current),Dock=DockStyle.Fill});UiTheme.Combo(versions);layout.Controls.Add(versions);layout.Controls.Add(message);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};UiTheme.Button(download,download.Text,"refresh");download.Width=280;UiTheme.Button(folder,folder.Text,"pip");folder.Width=200;actions.Controls.Add(download);actions.Controls.Add(folder);layout.Controls.Add(actions);
        versions.AccessibleName=L.T("Versões estáveis");versions.SelectedIndexChanged+=(_,_)=>{download.Enabled=!busy&&versions.SelectedItem!=null;message.Text=L.T("Escolha a versão atual ou uma anterior para recuperação. O download verifica SHA-256 e salva uma cópia das preferências. Assinatura pública pendente.");};
        Shown+=async (_,_)=>{
            try{busy=true;message.Text=L.T("Verificando atualizações…");var releases=await VerifiedUpdates.List(cancellation.Token);if(IsDisposed)return;versions.Items.AddRange(releases);busy=false;if(versions.Items.Count>0)versions.SelectedIndex=0;else message.Text=L.T("Não foi possível verificar atualizações agora.");}
            catch(Exception ex)when(ex is System.Net.Http.HttpRequestException or OperationCanceledException or System.Text.Json.JsonException){if(!IsDisposed)message.Text=L.T("Não foi possível verificar atualizações agora.");}
            finally{busy=false;}
        };
        download.Click+=async (_,_)=>await Download();
        folder.Click+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(root,"updates")){UseShellExecute=true});
        FormClosing+=(_,_)=>cancellation.Cancel();
    }
    async Task Download(){
        if(busy||versions.SelectedItem is not InstallerRelease release)return;busy=true;download.Enabled=versions.Enabled=false;folder.Visible=false;
        try{
            string backup=Path.Combine(root,"backups","before-update-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json");Directory.CreateDirectory(Path.GetDirectoryName(backup)!);PreferencesArchive.Export(root,backup);
            using var client=VerifiedUpdates.Client();await VerifiedUpdates.Download(client,release,Path.Combine(root,"updates"),new Progress<int>(p=>{if(busy&&!IsDisposed)message.Text=L.F("Baixando e verificando: {0}%",p);}),cancellation.Token);
            if(IsDisposed)return;folder.Visible=true;message.Text=L.T("Download verificado. Feche o PiP Overlay e execute o instalador na pasta. Se precisar, escolha uma versão anterior aqui e importe a cópia em data/backups.");
        }catch(Exception ex)when(ex is System.Net.Http.HttpRequestException or IOException or UnauthorizedAccessException or OperationCanceledException){if(!IsDisposed)message.Text=L.T("Download não concluído ou integridade inválida. Nenhum instalador foi executado.");}
        finally{busy=false;if(!IsDisposed){versions.Enabled=true;download.Enabled=versions.SelectedItem!=null;}}
    }
    protected override void Dispose(bool disposing){if(disposing)cancellation.Cancel();base.Dispose(disposing);}
}
