using System.IO.Compression;
using System.Reflection;

internal static class Program
{
    internal static void Install(string destination)
    {
        destination=Path.GetFullPath(destination);
        if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Esta pasta já existe. Escolha um nome novo para preservar seus arquivos.");
        string parent=Path.GetDirectoryName(destination)!;
        if(!Directory.Exists(parent))throw new IOException("Selecione uma pasta principal que já exista.");
        string staging=Path.Combine(parent,".pip-install-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try {
            using var payload=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")!;
            using var archive=new ZipArchive(payload,ZipArchiveMode.Read);
            archive.ExtractToDirectory(staging);
            foreach(string file in new[]{"PipOverlay.exe","coreclr.dll","hostfxr.dll","PipGpu.dll","PipRender32.dll","PipRender64.dll","PipProbe32.exe","PipProbe64.exe","graphics/ReShade32.dll","graphics/ReShade64.dll","graphics/PipOverlay.addon32","graphics/PipOverlay.addon64","graphics/PipVulkan32.dll","graphics/PipVulkan64.dll","graphics/PipLaunch32.exe","graphics/PipLaunch64.exe","effects/PipOverlay.fx"})
                if(!File.Exists(Path.Combine(staging,file)))throw new IOException("Pacote incompleto: "+file);
            Directory.Move(staging,destination);
        } finally { if(Directory.Exists(staging))Directory.Delete(staging,true); }
    }

    [STAThread] static int Main(string[] args)
    {
        // Unattended extraction supports isolated packaging validation; never launches the app.
        if(args.Length==2&&args[0]=="--extract") {
            try {Install(args[1]);return 0;}catch{return 1;}
        }
        ApplicationConfiguration.Initialize();
        using var form=new Form{Text="Instalar PiP Overlay",ClientSize=new Size(580,320),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,BackColor=Color.FromArgb(16,22,32),ForeColor=Color.White,Font=new Font("Segoe UI",10)};
        form.Icon=Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        var heading=new Label{Text="PiP Overlay",Font=new Font("Segoe UI",22,FontStyle.Bold),AutoSize=true,Location=new Point(24,20)};
        var description=new Label{Text="Tudo em um instalador. Sem extensão ou cadastro.\nAbra seu jogo pela ferramenta e o PiP do navegador.",AutoSize=true,Location=new Point(26,76)};
        var path=new TextBox{Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"PiP Overlay 0.11"),Location=new Point(26,139),Width=410};
        var browse=new Button{Text="Escolher…",Location=new Point(445,137),Size=new Size(108,32)};
        var status=new Label{Text="Escolha uma pasta nova. Para remover, basta excluir essa pasta.\nNão é necessário fechar os jogos para abrir a ferramenta.",Location=new Point(26,184),Size=new Size(530,58)};
        var install=new Button{Text="Instalar",Location=new Point(393,260),Size=new Size(160,38),BackColor=Color.FromArgb(91,210,187),ForeColor=Color.Black,FlatStyle=FlatStyle.Flat};
        browse.Click+=(_,_)=>{using var picker=new FolderBrowserDialog{Description="Escolha onde criar a pasta PiP Overlay"};if(picker.ShowDialog(form)==DialogResult.OK)path.Text=Path.Combine(picker.SelectedPath,"PiP Overlay");};
        var launch=new CheckBox{Text="Abrir PiP Overlay ao concluir",Checked=true,AutoSize=true,Location=new Point(26,267)};
        bool completed=false;string? installedPath=null;
        install.Click+=async (_,_)=>{
            if(completed){
                if(launch.Checked)try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(installedPath!,"PipOverlay.exe")){UseShellExecute=true,WorkingDirectory=installedPath});}catch(Exception ex){status.Text="Instalado. Não foi possível abrir: "+ex.Message;launch.Checked=false;return;}
                form.Close();return;
            }
            install.Enabled=browse.Enabled=path.Enabled=false;
            string destination=path.Text;
            status.Text="Extraindo os arquivos…";
            try {
                await Task.Run(()=>Install(destination));
                installedPath=destination;
                status.Text="Pronto. Clique em Concluir para começar.\nNão há extensão nem outro pacote para instalar.";
                install.Text="Concluir";install.Enabled=true;
                completed=true;
            }catch(Exception ex){status.Text=ex.Message;install.Enabled=browse.Enabled=path.Enabled=true;}
        };
        form.Controls.AddRange(new Control[]{heading,description,path,browse,status,launch,install});
        Application.Run(form);return 0;
    }
}
